namespace Umbraco.Community.CachePeek.Abstractions;

/// <summary>
/// Enumerates the keys HybridCache is holding in its local (L1) tier.
/// </summary>
public interface IHybridCacheKeySource
{
    /// <summary>
    /// Gets a value indicating whether enumeration is possible in this application.
    /// </summary>
    /// <remarks>
    /// Enumeration requires the registered <c>IMemoryCache</c> to be the concrete
    /// <c>MemoryCache</c>, which is what <c>DefaultHybridCache</c> resolves out of DI. A site that
    /// substitutes its own implementation loses the key list but keeps every other feature.
    /// </remarks>
    bool CanEnumerate { get; }

    /// <summary>
    /// Lazily walks the local cache, yielding only keys owned by HybridCache.
    /// </summary>
    /// <remarks>
    /// The walk is lazy on purpose: callers apply filtering and paging during enumeration so a large
    /// cache is never materialised into a response. Entries belonging to other
    /// <c>IMemoryCache</c> consumers are excluded, and are never yielded even in redacted form —
    /// see ADR-0006.
    /// </remarks>
    IEnumerable<string> EnumerateKeys(CancellationToken cancellationToken);
}
