namespace Umbraco.Community.CachePeek.Abstractions;

/// <summary>
/// Reads a cached value without changing what is cached.
/// </summary>
public interface IHybridCacheReader
{
    /// <summary>
    /// Reads the value stored under <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="consultDistributedCache">
    /// Whether the read may fall through to the distributed (L2) tier. Listing passes
    /// <see langword="false"/> so paging a thousand keys can never become a thousand network round
    /// trips; the single-entry view passes <see langword="true"/>.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The cached value, or <see langword="null"/> for both a miss and a genuinely cached null.
    /// Callers distinguish the two using <see cref="CacheEntryDescriptor.Resident"/>.
    /// </returns>
    ValueTask<object?> ReadAsync(string key, bool consultDistributedCache, CancellationToken cancellationToken);
}
