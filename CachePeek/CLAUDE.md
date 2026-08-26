# CachePeek

An Umbraco 17+ community package that inspects and evicts entries in the .NET HybridCache that
Umbraco's published content cache runs on.

## Non-negotiables

These are not style preferences. Each one is load-bearing, and each has an ADR in
`docs/decisions` explaining what it protects.

1. **Never surface a cache entry that has not positively proved it belongs to HybridCache.**
   The local tier is the application's shared `IMemoryCache`, and Umbraco stores backoffice
   external-login links in it *keyed by the one-time secret*. Filtering is positive-only; a
   denylist fails open. Do not add a fallback that shows unidentified entries, not even redacted,
   not even as a count of keys. (ADR-0006)

2. **Never conflate "resident" with "live".** `RemoveByTagAsync` records a timestamp and leaves
   entries physically in the cache. Any API or view that reports one as the other is a bug.
   (ADR-0003)

3. **Never put code on the read or write path of a content request.** No `HybridCache` decorator,
   no hosted service, no timer, no startup scan. Everything is request-scoped and admin-triggered.
   The benchmark comparing an idle install with and without the package is the acceptance gate.
   (ADR-0005)

4. **Never sniff versions.** The package spans Umbraco 17 and 18, whose cache behaviour differs.
   Probe for the capability, never branch on an assembly version. (ADR-0018)

5. **Reflection is for type discovery and optional metadata only.** Values are read through the
   public `GetOrCreateAsync` API. Every reflective enricher is individually guarded and degrades to
   "unavailable" rather than failing the request. (ADR-0007)

## Pinned assumptions

`tests/…/PinnedAssumptionsTests.cs` asserts behaviour of `Microsoft.Extensions.Caching.Hybrid` that
is implementation detail, not contract. If one fails after a dependency upgrade, **read the ADR
before changing the test** — the failure means the dependency moved, and the fix is usually in the
package, not in the assertion.

## Layout

- `src/Umbraco.Community.CachePeek` — the package. Ships the compiled backoffice assets as static
  web assets, so consumers only run `dotnet add package`.
- `tests/Umbraco.Community.CachePeek.Tests` — unit and pinned-assumption tests.
- `docs/decisions` — one ADR per decision. If you change a decision, update its ADR in the same PR.

## Conventions

- Central package management: versions live in `Directory.Packages.props`, never in a csproj.
- Warnings are errors. XML docs are required on public members.
- Build against the lowest supported Umbraco (17.0.0); everything newer is a capability probe.
