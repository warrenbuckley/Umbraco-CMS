# ADR-0013: Farm visibility is read from the database, and is partial by design

**Status:** accepted — revised after upstream #22145

## Context

`IServerMessenger` is one-way: every method returns `void` and there is no response channel. A
server can broadcast a command to the farm but cannot ask it a question, so per-node cache contents
are not obtainable by messaging.

The database knows more than the messenger does. Servers record their instruction-processing
position in `umbracoLastSynced`, keyed by machine, which any server can read. Combined with
`IServerRegistrationService.GetActiveServers()` and `ICacheInstructionService.GetMaxInstructionId()`,
that yields live per-node lag — turning "did my eviction land everywhere?" into a number.

The first draft of this decision claimed any server could see every other server's position. That is
false. Upstream [#22145](https://github.com/umbraco/Umbraco-CMS/pull/22145) routes subscriber servers
with a read-only database to local disk instead:

```csharp
private ILastSyncedRepository GetRepository()
{
    if (_serverRoleAccessor.Value.CurrentServerRole != ServerRole.Subscriber)
    {
        return _databaseRepository;
    }

    return _databaseReadOnlyAccessor.IsReadOnly()
        ? _fileSystemRepository
        : _databaseRepository;
}
```

A read-only database is a normal, correct configuration for a content-delivery node — which is
exactly the population most worth checking.

## Decision

Build the farm view from the database, and be explicit about its coverage. A node whose position is
not centrally recorded is reported as **"position not centrally visible — read-only database"**.
Never as zero. Never as behind. `IServerRoleAccessor` and `IDatabaseReadOnlyAccessor` make the
distinction detectable, so this is a labelling problem, not a dead end.

A second correction applies: `umbracoLastSynced.machineId` is `{machineName}/{siteName}` since
upstream #22257, while `umbracoServer.serverIdentity` uses a different scheme. Correlating the
roster to the sync positions is a fuzzy match, and unmatched rows on either side must be shown as
unmatched rather than dropped.

## Consequences

The farm view is genuinely useful and honestly incomplete. A view that silently showed CD servers as
permanently lagging would be worse than not shipping one.

## Breaks if

`ServerRoleAwareLastSyncedRepository`'s routing rule changes — the coverage of the view changes with
it. Watch that file on every Umbraco minor, not just majors.
