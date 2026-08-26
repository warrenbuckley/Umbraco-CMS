using System.Reflection;

namespace Umbraco.Community.CachePeek.Inspection;

/// <summary>
/// Decides whether an object taken out of the shared memory cache belongs to HybridCache, and if so
/// what type it wraps.
/// </summary>
/// <remarks>
/// <para>
/// This is the security boundary of the whole package, and it is deliberately a positive test rather
/// than a denylist. <c>DefaultHybridCache</c> resolves its local tier from DI, so it shares the
/// application's <c>IMemoryCache</c> with every other consumer — including
/// <c>BackOfficeExternalLoginService</c>, which stores external-login links keyed by the one-time
/// secret itself. Enumerating that cache without filtering would publish credentials.
/// </para>
/// <para>
/// A denylist of known-sensitive keys fails open the moment anyone adds a new <c>IMemoryCache</c>
/// consumer, so entries are shown only when they positively prove they are ours. When the test
/// cannot be performed, the entry is omitted — never shown, never counted by key. See ADR-0006.
/// </para>
/// </remarks>
internal static class HybridCacheEntryIdentifier
{
    private const string CacheItemTypeName = "CacheItem";
    private const string OwningTypeName = "DefaultHybridCache";
    private const string OwningAssemblyName = "Microsoft.Extensions.Caching.Hybrid";

    /// <summary>
    /// Determines whether <paramref name="cacheEntry"/> is a HybridCache cache item, and reports the
    /// type of the value it holds.
    /// </summary>
    /// <param name="cacheEntry">The raw object retrieved from the memory cache.</param>
    /// <param name="valueType">The wrapped value type, when identification succeeded.</param>
    /// <returns><see langword="true"/> only when the entry provably belongs to HybridCache.</returns>
    public static bool TryIdentify(object? cacheEntry, out Type? valueType)
    {
        valueType = null;

        if (cacheEntry is null)
        {
            return false;
        }

        var owned = false;

        // Walk the inheritance chain looking for DefaultHybridCache.CacheItem<T> and, above it, the
        // non-generic DefaultHybridCache.CacheItem. Both concrete items (ImmutableCacheItem<T> and
        // MutableCacheItem<T>) derive from them, so this covers every entry HybridCache writes
        // without naming the concrete types, which are free to change.
        for (Type? candidate = cacheEntry.GetType(); candidate is not null; candidate = candidate.BaseType)
        {
            if (IsOwnedBy(candidate) is false || candidate.Name.StartsWith(CacheItemTypeName, StringComparison.Ordinal) is false)
            {
                continue;
            }

            owned = true;

            if (valueType is null && candidate.IsGenericType)
            {
                Type[] arguments = candidate.GetGenericArguments();
                if (arguments.Length == 1)
                {
                    valueType = arguments[0];
                }
            }
        }

        if (owned is false)
        {
            valueType = null;
        }

        return owned;
    }

    /// <summary>
    /// Confirms a type is nested inside <c>DefaultHybridCache</c> in the real HybridCache assembly,
    /// so an unrelated type that merely happens to be called <c>CacheItem</c> cannot pass the test.
    /// </summary>
    private static bool IsOwnedBy(Type candidate)
    {
        Type declaring = candidate.DeclaringType ?? candidate;

        if (string.Equals(declaring.Name, OwningTypeName, StringComparison.Ordinal) is false)
        {
            return false;
        }

        AssemblyName assembly = candidate.Assembly.GetName();
        return string.Equals(assembly.Name, OwningAssemblyName, StringComparison.Ordinal);
    }
}
