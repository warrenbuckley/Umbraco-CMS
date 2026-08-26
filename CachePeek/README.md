# CachePeek

See what is actually in Umbraco's HybridCache — inspect keys and values, and evict them, from the
backoffice.

> **Status: pre-alpha.** The scaffolding and the pinned-assumption tests exist. The Management API
> and the backoffice UI do not yet. See [the plan](#design) before contributing.

## Why

`HybridCache` deliberately exposes no way to enumerate what it holds. That is the right call for a
cache API and an awkward one when a site is serving stale content and nobody can see why. Umbraco's
own diagnostics job says as much, in a source comment:

> The HybridCache L1 (Microsoft's in-process tier of ContentCacheNode entries, behind L0) does not
> expose an entry count; capture it from a GC dump when a finer breakdown is needed.

CachePeek fills that gap, without reflection on the paths that matter and without touching the hot
path of a single content request.

## Supported versions

Umbraco 17.0 and up, including 18. One build serves both: everything version-specific is a runtime
capability probe, never a version check (ADR-0018).

## Design

The full research and build plan, including the nineteen decisions this package is built on and the
evidence behind each, lives with the project. Every decision has an ADR in
[`docs/decisions`](docs/decisions), and each ADR names the test that pins it.

Three things are worth knowing before reading any code:

1. **Resident is not live.** HybridCache invalidates by tag lazily, so entries survive a cache clear
   until something reads them. A key list on its own reports a full cache that is entirely dead.
2. **The local tier is shared.** It is the application's `IMemoryCache`, which also holds
   backoffice login secrets. Filtering is a security boundary, not a tidiness feature.
3. **Nothing runs unless you look.** No hosted service, no timer, no decorator, no startup work.

## Contributing

If you change a decision, update its ADR in the same pull request.
