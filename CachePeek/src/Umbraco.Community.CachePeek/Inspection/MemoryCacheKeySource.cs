using Microsoft.Extensions.Caching.Memory;
using Umbraco.Community.CachePeek.Abstractions;

namespace Umbraco.Community.CachePeek.Inspection;

/// <summary>
/// Enumerates HybridCache's local tier by reading the application's shared memory cache.
/// </summary>
/// <remarks>
/// <para>
/// <c>HybridCache</c> exposes no enumeration API, which looks like a dead end and is the usual reason
/// to reach for reflection. It is not necessary: <c>DefaultHybridCache</c> obtains its local tier
/// with <c>services.GetRequiredService&lt;IMemoryCache&gt;()</c>, so the L1 store *is* the shared
/// singleton, keys go in unprefixed, and <c>MemoryCache.Keys</c> has been public since .NET 9.
/// See ADR-0001.
/// </para>
/// </remarks>
internal sealed class MemoryCacheKeySource : IHybridCacheKeySource
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheKeySource(IMemoryCache memoryCache) => _memoryCache = memoryCache;

    /// <inheritdoc />
    public bool CanEnumerate => _memoryCache is MemoryCache;

    /// <inheritdoc />
    public IEnumerable<string> EnumerateKeys(CancellationToken cancellationToken)
    {
        if (_memoryCache is not MemoryCache memoryCache)
        {
            yield break;
        }

        foreach (object key in memoryCache.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // HybridCache only ever writes string keys. Anything else belongs to another consumer.
            if (key is not string stringKey)
            {
                continue;
            }

            // MemoryCache.Keys yields raw dictionary keys with no expiry check, so entries that have
            // expired but not yet been swept still appear. TryGetValue does honour expiry, and it
            // also hands back the entry we need for the ownership test below.
            if (memoryCache.TryGetValue(stringKey, out object? entry) is false)
            {
                continue;
            }

            if (HybridCacheEntryIdentifier.TryIdentify(entry, out _) is false)
            {
                continue;
            }

            yield return stringKey;
        }
    }
}
