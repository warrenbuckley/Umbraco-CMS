# ADR-0002: Read values with `DisableUnderlyingData`

**Status:** accepted

## Context

Reading a cached value needs a `TryGetValue`, which `HybridCache` does not offer. The obvious
workaround — `GetOrCreateAsync` with a factory that reports a miss — writes an entry and then
deletes it. That is what Umbraco's own internal `TryGetValueAsync` extension does, complete with a
per-key `SemaphoreSlim`; against a Redis-backed L2 it means a real write and delete on every miss.

`HybridCacheEntryFlags.DisableUnderlyingData` short-circuits the factory, and the miss path is
explicitly write-free. From `DefaultHybridCache.StampedeState`:

```csharp
private void SetDefaultResult()
{
    // note we don't store this dummy result in L1 or L2
    ...
}
```

## Decision

Read through `GetOrCreateAsync` with `DisableUnderlyingData`, plus `DisableLocalCacheWrite` and
`DisableDistributedCacheWrite` so the intent survives even if that implementation detail changes.
Listing additionally sets `DisableDistributedCacheRead`, so paging a thousand keys can never become
a thousand network round trips.

The value type is not known at compile time. It is discovered from the resident cache item's
generic argument and a closed generic reader is built once per type; the read itself is public API.

## Consequences

Browsing the cache does not populate it. A miss and a genuinely cached null are indistinguishable
from the return value alone, so callers disambiguate using residency, which is known separately.

An L2-only entry has no local cache item, so there is no type to close the generic over — such a
key can be named but not rendered, and the UI says so rather than showing an empty panel.

## Breaks if

`SetDefaultResult` starts caching the default value.

## Pinned by

`PinnedAssumptionsTests.Reading_A_Missing_Key_Does_Not_Write_Anything`
