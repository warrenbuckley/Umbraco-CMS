# ADR-0019: Register the HybridCache L1 as an `IMemoryCacheSizeReporter`

**Status:** accepted (Umbraco 18 and later only)

## Context

Umbraco 18 added `IMemoryCacheSizeReporter` to Core — a public abstraction with `CacheName`,
`GetApproximateCount()` and an optional `GetApproximateBytes()`. Seven caches implement it: the
document, media and element cache services, the document URL cache, and the three navigation
services. One consumer reads it: `MemoryCacheSizeReportingJob`, a one-minute recurring job that
does nothing at all unless debug logging is enabled.

The job's closing comment names the gap:

```csharp
// The HybridCache L1 (Microsoft's in-process tier of ContentCacheNode entries, behind L0)
// does not expose an entry count; capture it from a GC dump when a finer breakdown is
// needed. The process totals below give the overall picture.
```

That is Umbraco documenting, in source, the exact hole this package exists to fill — and reaching
for a GC dump as the workaround.

## Decision

Where the interface is present, register the HybridCache L1 as an `IMemoryCacheSizeReporter`.
Counting resident `CacheItem` entries is work the package does anyway.

The interface does not exist on Umbraco 17, so registration is a capability probe (ADR-0018), not a
dependency.

## Consequences

The L1 entry count appears in Umbraco's own diagnostics output alongside the seven built-in caches,
benefiting people who never open the dashboard and have only a log to go on. It is also the natural
shape for an eventual core contribution, because the abstraction already exists upstream.

## Breaks if

Counting L1 stops being cheap. The reporter must stay a count — never a scan that materialises
values — or it violates the performance budget in ADR-0005.
