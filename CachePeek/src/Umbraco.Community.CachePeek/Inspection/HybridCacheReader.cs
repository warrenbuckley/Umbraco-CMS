using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Community.CachePeek.Abstractions;

namespace Umbraco.Community.CachePeek.Inspection;

/// <summary>
/// Reads cached values through HybridCache's public API without writing anything back.
/// </summary>
/// <remarks>
/// <para>
/// <c>GetOrCreateAsync</c> with <see cref="HybridCacheEntryFlags.DisableUnderlyingData"/> never runs
/// the factory, and its miss path is explicitly write-free — <c>DefaultHybridCache</c>'s
/// <c>SetDefaultResult</c> carries the comment <c>"note we don't store this dummy result in L1 or
/// L2"</c>. That turns the flag into the <c>TryGetValue</c> that HybridCache does not otherwise
/// offer. The write-disabling flags are set as well, so the intent survives even if that
/// implementation detail changes. See ADR-0002.
/// </para>
/// <para>
/// The value type is not known at compile time, so it is discovered from the cache item itself and
/// a closed generic reader is built once per type. Reflection is confined to that discovery; the
/// read is ordinary public API.
/// </para>
/// </remarks>
internal sealed class HybridCacheReader : IHybridCacheReader
{
    private static readonly ConcurrentDictionary<Type, ITypedReader> Readers = new();

    private readonly HybridCache _hybridCache;
    private readonly IMemoryCache _memoryCache;

    public HybridCacheReader(HybridCache hybridCache, IMemoryCache memoryCache)
    {
        _hybridCache = hybridCache;
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public ValueTask<object?> ReadAsync(string key, bool consultDistributedCache, CancellationToken cancellationToken)
    {
        // The value type comes from the resident cache item. Without one there is nothing to close
        // the generic over — an L2-only entry can be named but not rendered.
        if (_memoryCache.TryGetValue(key, out object? entry) is false
            || HybridCacheEntryIdentifier.TryIdentify(entry, out Type? valueType) is false
            || valueType is null)
        {
            return ValueTask.FromResult<object?>(null);
        }

        ITypedReader reader = Readers.GetOrAdd(
            valueType,
            static type => (ITypedReader)Activator.CreateInstance(typeof(TypedReader<>).MakeGenericType(type))!);

        return reader.ReadAsync(_hybridCache, key, BuildOptions(consultDistributedCache), cancellationToken);
    }

    private static HybridCacheEntryOptions BuildOptions(bool consultDistributedCache)
    {
        HybridCacheEntryFlags flags = HybridCacheEntryFlags.DisableUnderlyingData
            | HybridCacheEntryFlags.DisableLocalCacheWrite
            | HybridCacheEntryFlags.DisableDistributedCacheWrite;

        if (consultDistributedCache is false)
        {
            flags |= HybridCacheEntryFlags.DisableDistributedCacheRead;
        }

        return new HybridCacheEntryOptions { Flags = flags };
    }

    private interface ITypedReader
    {
        ValueTask<object?> ReadAsync(HybridCache cache, string key, HybridCacheEntryOptions options, CancellationToken cancellationToken);
    }

    private sealed class TypedReader<T> : ITypedReader
    {
        public async ValueTask<object?> ReadAsync(HybridCache cache, string key, HybridCacheEntryOptions options, CancellationToken cancellationToken)
        {
            T value = await cache
                .GetOrCreateAsync(
                    key,
                    static (CancellationToken _) => ValueTask.FromResult<T>(default!),
                    options,
                    tags: null,
                    cancellationToken)
                .ConfigureAwait(false);

            return value;
        }
    }
}
