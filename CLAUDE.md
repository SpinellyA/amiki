# Amiki: project notes

These override the defaults in the shared `../CLAUDE.md` where they differ. Decided together on
2026-10-27.

## Architecture: "Clean-lite"

Clean Architecture boundaries, without ceremony where there's no logic.

```
Amiki.Domain          Entities, value objects, domain rules. Pure C#, no packages.
                      Shared with the Blazor client (it works offline on the same classes).
Amiki.Contracts       Requests/responses that cross the wire (records). Shared with the client.
Amiki.Application     Use cases per feature (Entities/, Finance/, Assistant/, People/) and the
                      ports they need: IAmikiDb, ICurrentUser, ILanguageModel, ISecretProtector...
Amiki.Infrastructure  EF Core (AmikiDb, migrations), Gemini, data protection, dev seed.
Amiki.Api             Thin HTTP: endpoints, auth, middleware, composition root.
Amiki.Client          Blazor WASM (MudBlazor).
Amiki.Tests           xUnit: unit tests + integration tests (Testcontainers Postgres).
```

Dependencies point inward only: Domain ← Contracts ← Application ← Infrastructure ← Api.
Project references enforce it: Domain code can't see EF Core because it doesn't reference it.

### Deliberate deviations from the shared defaults

- **No MediatR.** Application services (one class per use case or feature) do the same job
  without the extra indirection. (MediatR's newer versions also need a license check.)
- **Minimal APIs, not FastEndpoints.** Each feature maps its own routes (`MapFinance()`, ...).
  Simple "save this object for this person" entities share one generic endpoint; their rules
  live in an `IEntityRules<T>` class per entity in Application, never in Program.cs.
- **No repository per entity.** `IAmikiDb` (EF's DbSets) is the data port. The per-person query
  filter makes it safe to use directly. Cross-person admin work goes through `IPeopleData`.

### Rules of thumb

- A rule about the business goes in Domain (on the type it's about) or Application. Never in an
  endpoint, never duplicated between client and server.
- No grab-bag "helper"/"utils" classes. Logic lives on the type it belongs to, or in a small
  class named after what it is (`Week`, `Ledger`, `ToolArguments`).
- No static mutable state; use an injected singleton.
- Time comes from the injected `TimeProvider` (set to Amiki's time zone), never `DateTime.Now`
  on the server: the server runs in UTC.
- Every person's data is separated by the `UserId` query filter in `AmikiDb`. The diary is
  never sent to the language model and never shown to the owner.
