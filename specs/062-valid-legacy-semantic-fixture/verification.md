# Verification: Valid Legacy Semantic Fixture

## Fixture checks

- `dotnet restore examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj --force-evaluate` — passed; NHibernate 5.5.2 and transitive packages resolved from `https://api.nuget.org/v3/index.json`.
- `dotnet build examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj --no-restore` — passed, 0 warnings and 0 errors.
- `dotnet list examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj package --vulnerable --include-transitive` — no vulnerable packages reported by the current NuGet sources.
- `dotnet run --project src/BrownPants.Cli -c Release --no-build -- inspect examples/brownfield-bug-lab/legacy/Framework --semantic --format json --limit 1000` — 1/1 project loaded, 0 failed, coverage complete, 0 workspace issues, 0 compiler errors, 4 resolved invocations, 0 unresolved invocations.

Resolved targets were:

| Source | Roslyn target |
|---|---|
| `OrderDataAccess.cs:13` `_sessions.OpenSession` | `NHibernate.ISessionFactory.OpenSession()` |
| `OrderDataAccess.cs:14` `session.BeginTransaction` | `NHibernate.ISession.BeginTransaction()` |
| `OrderDataAccess.cs:15` `session.SaveOrUpdate` | `NHibernate.ISession.SaveOrUpdate(object)` |
| `OrderDataAccess.cs:16` `transaction.Commit` | `NHibernate.ITransaction.Commit()` |

## Repository regression checks

- Managed Bug Lab regression command — passed: `LegacyOrderDesk checks passed.`
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 282 tests.
- BrownPants-on-BrownPants semantic inspection — 5/5 projects loaded, 0 failed, complete coverage, 0 workspace issues, 0 compiler errors, 10,358 resolved invocations, 0 unresolved invocations.
- Acceptance criteria hash — passed: `36fe59661264d9cbc3a223019b7de327e436f3d7a73a741e5b87f2490951ae53`.
- Acceptance evidence ledger — 3 tasks map to all 6 criteria.

## Environment limits

The sample remains a classic .NET Framework 4.7.2 project with a Windows runtime identifier. The semantic result above was observed on Windows with the .NET Framework targeting assemblies available. Other hosts must report incomplete coverage if they cannot evaluate that legacy target. Semantic mode remains opt-in because MSBuild imports may evaluate build targets.
