# ADR-0016: MainDom status is a first-class check

**Status:** accepted

## Context

Upstream [#23421](https://github.com/umbraco/Umbraco-CMS/pull/23421) decoupled *writing* cache
instructions from MainDom registration, because an instance that lost the MainDom race during an app
pool recycle or slot swap would otherwise stop broadcasting its own changes and peers would never
refresh.

*Processing* instructions remains MainDom-gated, deliberately. The comment in
`DatabaseServerMessenger.RequiresDistributed` is explicit that only the write half was ungated. So a
node that failed MainDom registration still never processes instructions, and says so:

> Could not register with MainDom; this instance will not process distributed cache instructions and
> its published cache may become stale until the application is restarted.

That node's local cache is stale until the process restarts. No expiry corrects it. No eviction
reaches it. Today the only symptom is one error line in a log nobody is reading.

## Decision

Report MainDom status prominently, not as a footnote. `IMainDom.IsMainDom` is public, so it is
directly reportable for the server handling the request, and inferable for remote nodes from a
`lastSyncedDate` that has stopped advancing (within the coverage limits of ADR-0013).

## Consequences

This is probably the highest-value single check in the package: a silent, restart-until-fixed
staleness bug that no existing tool surfaces.

## Breaks if

Instruction processing stops being MainDom-gated. #23421 ungated the write half and left the
processing half alone on purpose, so treat any movement here as a signal to re-read
`DatabaseServerMessenger` rather than to delete the check.
