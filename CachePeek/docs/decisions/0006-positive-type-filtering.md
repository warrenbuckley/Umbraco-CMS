# ADR-0006: Positive type filtering, never a denylist

**Status:** accepted — security boundary, do not relax

## Context

Because the local tier is the application's shared `IMemoryCache` (ADR-0001), enumerating it reaches
every other consumer's entries too. Including this one, from Umbraco's own
`BackOfficeExternalLoginService`:

```csharp
_memoryCache.Set(secret, new LoginProviderUserLink { ... }, new MemoryCacheEntryOptions { ... });
```

The cache **key** is the one-time external-login secret. Listing keys is listing secrets. Any
future `IMemoryCache` consumer may do something equally sensitive without anyone updating this
package.

## Decision

An entry is surfaced only when it positively proves it belongs to HybridCache: walking its
inheritance chain to a type named `CacheItem` that is nested in `DefaultHybridCache` **and** declared
in the `Microsoft.Extensions.Caching.Hybrid` assembly, so an unrelated type of the same name cannot
spoof it.

There is no denylist of known-sensitive keys. A denylist fails open the moment someone adds a
consumer nobody thought of, and failing open here means leaking credentials.

When the test cannot be performed, the entry is omitted. Not shown, not redacted, not counted by
key.

## Consequences

An unrecognised HybridCache internal shape would cause under-reporting rather than a leak. That is
the correct direction to fail.

## Breaks if

Never. This one is load-bearing for security and is not to be relaxed for convenience or for
completeness of the key list.

## Pinned by

`PinnedAssumptionsTests.Entries_Owned_By_Other_Consumers_Are_Never_Enumerated`
