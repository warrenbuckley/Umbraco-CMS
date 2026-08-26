# Decision records

One file per decision. Each states what was decided, the evidence behind it, what would invalidate
it, and the test that pins it.

Several of these rest on implementation details of `Microsoft.Extensions.Caching.Hybrid` and of
Umbraco's cache services rather than on published contracts. The **Breaks if** section of each is
the maintenance manual: on every major .NET or Umbraco upgrade, walk them and run the pinned tests
before doing anything else.

If you change a decision, update its ADR in the same pull request.

| ADR | Decision | Pinned by |
|-----|----------|-----------|
| [0001](0001-enumerate-via-shared-memory-cache.md) | Enumerate keys via the DI `IMemoryCache`, not reflection | `Local_Tier_Is_The_Shared_MemoryCache_And_Keys_Are_Unprefixed` |
| [0002](0002-non-mutating-reads.md) | Read values via `GetOrCreateAsync` + `DisableUnderlyingData` | `Reading_A_Missing_Key_Does_Not_Write_Anything` |
| [0003](0003-resident-is-not-live.md) | Distinguish resident from live, everywhere, always | `Tag_Invalidation_Leaves_Entries_Resident_But_Not_Live` |
| 0004 | Observe (read-only) is the default; Verify is opt-in | *pending* |
| 0005 | No `HybridCache` decorator in the default build | *pending — benchmark* |
| [0006](0006-positive-type-filtering.md) | Positive type filtering, never a denylist | `Entries_Owned_By_Other_Consumers_Are_Never_Enumerated` |
| 0007 | Reflection for type discovery and optional metadata only | *pending* |
| 0008 | Farm eviction via `ICacheRefresher`, not a bespoke transport | *pending* |
| 0009 | L2 evicted exactly once per farm operation | *pending* |
| 0010 | L0 cleared alongside every eviction | *pending* |
| 0011 | One NuGet with embedded static web assets | n/a |
| 0012 | Admin-only by default | *pending* |
| [0013](0013-farm-visibility-is-partial.md) | Farm visibility is read from the database, and is partial by design | *pending* |
| 0014 | No per-node cache telemetry collection | n/a |
| 0015 | Eviction UI reports "queued", never "done" | *pending* |
| [0016](0016-maindom-is-a-first-class-check.md) | MainDom status is a first-class check | *pending* |
| 0017 | Use `IMemoryCacheSizeReporter` where present | *pending* |
| 0018 | Capability detection, never version sniffing | *pending* |
| [0019](0019-report-l1-size-upstream.md) | Register L1 as an `IMemoryCacheSizeReporter` | *pending* |
