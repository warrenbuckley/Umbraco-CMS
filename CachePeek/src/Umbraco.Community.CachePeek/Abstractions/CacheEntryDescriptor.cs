namespace Umbraco.Community.CachePeek.Abstractions;

/// <summary>
/// The observed state of a single cache key.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Resident"/> and <see cref="Live"/> are deliberately separate, and callers must never
/// conflate them. HybridCache invalidates by tag lazily — <c>RemoveByTagAsync</c> records a timestamp
/// and leaves entries physically in the local cache until something reads them. Immediately after a
/// cache clear, every entry is still resident and none of it is live. See ADR-0003.
/// </para>
/// </remarks>
public sealed record CacheEntryDescriptor
{
    /// <summary>Gets the raw cache key, exactly as HybridCache stores it.</summary>
    public required string Key { get; init; }

    /// <summary>Gets a value indicating whether the key is physically present in the local cache.</summary>
    public required bool Resident { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry would actually be served to a reader, or
    /// <see langword="null"/> when liveness could not be determined without mutating the cache.
    /// </summary>
    public required bool? Live { get; init; }

    /// <summary>Gets the CLR type of the cached value, when it could be discovered.</summary>
    public Type? ValueType { get; init; }

    /// <summary>Gets the tags attached to the entry, when metadata enrichment is available.</summary>
    public IReadOnlyCollection<string> Tags { get; init; } = [];

    /// <summary>Gets the approximate serialized size in bytes, when known.</summary>
    public long? ApproximateBytes { get; init; }

    /// <summary>Gets when the entry was created, when known.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
