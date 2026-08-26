# ADR-0001: Enumerate keys via the shared `IMemoryCache`

**Status:** accepted

## Context

`HybridCache` exposes no enumeration: no `Keys`, no `TryGetValue`, no count. Listing what it holds
looks impossible without reflecting into `DefaultHybridCache`'s private state, which would tie the
package to .NET internals for its single most important feature.

It turns out not to be necessary. `DefaultHybridCache` does not own a private local tier:

```csharp
_localCache = services.GetRequiredService<IMemoryCache>();
```

The local tier *is* the application's shared `IMemoryCache` singleton. `MemoryCache.Keys` has been
public since .NET 9, and keys are written verbatim — `RemoveAsync` calls `_localCache.Remove(key)`
with no prefix.

## Decision

Enumerate by injecting `IMemoryCache`, casting to `MemoryCache`, and reading `Keys`. No reflection.

Two consequences follow and are handled elsewhere: the cache is shared with other consumers, so
every entry must pass a positive ownership test (ADR-0006); and `Keys` yields raw dictionary keys
with no expiry check, so each key needs a follow-up `TryGetValue`, which does honour expiry.

## Consequences

Key enumeration needs no private access, so it cannot break on a .NET patch that reshapes internal
fields. It does require the registered `IMemoryCache` to be the concrete `MemoryCache`; a site that
substitutes its own loses the key list and keeps everything else, reported honestly as
"enumeration unsupported" rather than as an empty cache.

## Breaks if

A future version gives HybridCache a private local cache. Detectable: the observed key count would
diverge from observed behaviour, and the pinned test fails outright. The fallback is reflection on
the internal `DefaultHybridCache.LocalCache` property, which exists today and is used by its own
test suite.

## Pinned by

`PinnedAssumptionsTests.Local_Tier_Is_The_Shared_MemoryCache_And_Keys_Are_Unprefixed`
