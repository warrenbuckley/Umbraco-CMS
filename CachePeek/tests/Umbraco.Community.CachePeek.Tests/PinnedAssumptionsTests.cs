using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Community.CachePeek.Inspection;

namespace Umbraco.Community.CachePeek.Tests;

/// <summary>
/// Pins the assumptions the package is built on.
/// </summary>
/// <remarks>
/// <para>
/// These are not ordinary unit tests. Each one asserts a behaviour of
/// <c>Microsoft.Extensions.Caching.Hybrid</c> that is an implementation detail rather than a
/// contract, and that the package would silently misbehave without. If one of these fails after a
/// dependency upgrade, stop and read the corresponding ADR before changing anything — the failure
/// means the world moved, not that the test is wrong.
/// </para>
/// </remarks>
[TestFixture]
public sealed class PinnedAssumptionsTests
{
    private ServiceProvider _services = null!;
    private HybridCache _hybridCache = null!;
    private MemoryCache _memoryCache = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
#pragma warning disable EXTEXP0018 // HybridCache builder APIs are marked experimental in some versions.
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        _services = services.BuildServiceProvider();
        _hybridCache = _services.GetRequiredService<HybridCache>();
        _memoryCache = (MemoryCache)_services.GetRequiredService<IMemoryCache>();
    }

    [TearDown]
    public void TearDown() => _services.Dispose();

    /// <summary>
    /// ADR-0001. HybridCache's local tier is the shared <c>IMemoryCache</c> from DI, and keys are
    /// stored verbatim. If this fails, key enumeration is no longer possible without reflection.
    /// </summary>
    [Test]
    public async Task Local_Tier_Is_The_Shared_MemoryCache_And_Keys_Are_Unprefixed()
    {
        const string key = "a1f3c0de-0000-0000-0000-000000000001";

        await _hybridCache.SetAsync(key, new Payload("hello"));

        Assert.That(_memoryCache.Keys.OfType<string>(), Does.Contain(key));
    }

    /// <summary>
    /// ADR-0002. A read with <see cref="HybridCacheEntryFlags.DisableUnderlyingData"/> must not
    /// create an entry on a miss. If this fails, browsing the cache would start populating it.
    /// </summary>
    [Test]
    public async Task Reading_A_Missing_Key_Does_Not_Write_Anything()
    {
        var options = new HybridCacheEntryOptions
        {
            Flags = HybridCacheEntryFlags.DisableUnderlyingData
                | HybridCacheEntryFlags.DisableLocalCacheWrite
                | HybridCacheEntryFlags.DisableDistributedCacheWrite,
        };

        int before = _memoryCache.Count;

        Payload? value = await _hybridCache.GetOrCreateAsync(
            "definitely-not-cached",
            static (CancellationToken _) => ValueTask.FromResult<Payload?>(null),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.Null, "the factory must not run");
            Assert.That(_memoryCache.Count, Is.EqualTo(before), "a miss must not create a cache entry");
        });
    }

    /// <summary>
    /// ADR-0006. Entries written by other <c>IMemoryCache</c> consumers must never be enumerated.
    /// This models <c>BackOfficeExternalLoginService</c>, which uses a one-time login secret as the
    /// cache key — enumerating it would publish credentials. This test is a security regression
    /// guard and must never be relaxed.
    /// </summary>
    [Test]
    public async Task Entries_Owned_By_Other_Consumers_Are_Never_Enumerated()
    {
        const string secret = "a-one-time-external-login-secret";
        const string ours = "b7e0c0de-0000-0000-0000-000000000002";

        _memoryCache.Set(secret, new { UserId = "some-user" }, TimeSpan.FromMinutes(1));
        await _hybridCache.SetAsync(ours, new Payload("ours"));

        var source = new MemoryCacheKeySource(_memoryCache);
        var keys = source.EnumerateKeys(CancellationToken.None).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(keys, Does.Contain(ours), "our own entry should be listed");
            Assert.That(keys, Does.Not.Contain(secret), "another consumer's entry must never be listed");
        });
    }

    /// <summary>
    /// ADR-0003. Tag invalidation is lazy: <c>RemoveByTagAsync</c> records a timestamp and leaves
    /// entries physically resident. This is why the package reports "resident" and "live" separately
    /// — after a cache clear, a naive key list would show a full cache that is entirely dead.
    /// </summary>
    [Test]
    public async Task Tag_Invalidation_Leaves_Entries_Resident_But_Not_Live()
    {
        const string key = "c204c0de-0000-0000-0000-000000000003";

        await _hybridCache.SetAsync(key, new Payload("doomed"), tags: ["content"]);
        Assume.That(_memoryCache.Keys.OfType<string>(), Does.Contain(key));

        await _hybridCache.RemoveByTagAsync("content");

        Assert.That(
            _memoryCache.Keys.OfType<string>(),
            Does.Contain(key),
            "tag invalidation is lazy — the entry is expected to still be physically present");

        var reader = new HybridCacheReader(_hybridCache, _memoryCache);
        object? value = await reader.ReadAsync(key, consultDistributedCache: false, CancellationToken.None);

        Assert.That(value, Is.Null, "a tag-invalidated entry must not be served");
    }

    /// <summary>
    /// The reader must round-trip a value whose type is not known at compile time.
    /// </summary>
    [Test]
    public async Task Reader_Materialises_A_Value_Of_A_Type_Discovered_At_Runtime()
    {
        const string key = "d3a7c0de-0000-0000-0000-000000000004";

        await _hybridCache.SetAsync(key, new Payload("round-trip"));

        var reader = new HybridCacheReader(_hybridCache, _memoryCache);
        object? value = await reader.ReadAsync(key, consultDistributedCache: false, CancellationToken.None);

        Assert.That(value, Is.TypeOf<Payload>());
        Assert.That(((Payload)value!).Message, Is.EqualTo("round-trip"));
    }

    private sealed record Payload(string Message);
}
