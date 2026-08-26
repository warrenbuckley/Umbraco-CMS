# ADR-0003: Resident is not live

**Status:** accepted

## Context

`RemoveByTagAsync` does not remove anything. It records an invalidation timestamp; entries are
checked lazily on read and dropped only then:

```csharp
public override ValueTask RemoveByTagAsync(string tag, CancellationToken token = default)
{
    long now = CurrentTimestamp();
    InvalidateTagLocalCore(tag, now, isNow: true);
    return InvalidateL2TagAsync(tag, now, token);
}
```

Umbraco's `ClearMemoryCacheAsync` is a `RemoveByTagAsync("content")`. So immediately after an editor
clicks "Reload published cache", every entry is still physically in the local cache and none of it
is usable. A tool that enumerates keys and stops there would report a full cache that is entirely
dead — and it would do so at exactly the moment someone is trying to work out whether their cache
clear worked.

## Decision

Presence and usability are separate questions with separate answers, structurally, throughout: in
the descriptor, in the API, and in the UI, where they get visually distinct treatment rather than a
footnote.

Liveness is resolved two ways. `DefaultHybridCache.IsValid(CacheItem)` is a public method on an
internal type, reachable by reflection, and is genuinely read-only. The public probe (ADR-0002) is
also truthful but *evicts* invalid entries as it finds them, because `TryGetExisting` removes them.
The reflective path is preferred; the probe is the guaranteed-correct fallback.

## Consequences

Every list response carries both states. The distinction is the single most useful thing the tool
reports, and the easiest to get subtly wrong.

## Breaks if

Never, in the sense that matters. If HybridCache switched to eager tag removal the distinction would
become trivially true rather than wrong.

## Pinned by

`PinnedAssumptionsTests.Tag_Invalidation_Leaves_Entries_Resident_But_Not_Live`
