# Verification: Semantic Source Path Boundaries

## Acceptance criteria integrity

- Baseline: `e4cae7f35805347675f566ab3e7ce2ddad6635bab5ec4bd6f3f7acb740e49b14`.
- The acceptance criteria digest was initialized and verified before implementation.

## BrownPants-on-BrownPants finding

- `dotnet run --project src/BrownPants.Cli -- inspect . --format json --limit 1000 --semantic` — discovered 2 .NET projects and loaded both. It resolved 3,699 invocation targets and reported 0 unresolved calls. The semantic declaration sample included a generated test SDK source path under the local `.nuget` cache.

## Implementation verification

- The external-source regression failed before implementation because the analyzer counted four declarations instead of the two repository declarations. After implementation, it passes with two declarations and one repository call; the call still resolves to `External.Remote.Execute` and has null `TargetPath` and `TargetLine`. A compilation assigned to an external project path is also excluded.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter FullyQualifiedName~SemanticAnalyzer` — passed, 5 tests.
- `dotnet build BrownPants.sln --configuration Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln --configuration Release --no-build` — passed, 91 tests.
- Acceptance hash, integrity workflow, review, and evidence PowerShell tests — passed. The feature ledger verifies 3 tasks and 8 criteria.
- Release self-inspection in both JSON and Markdown with `--semantic --limit 10000` — 2 projects loaded, 240 repository declarations, 3,749 resolved invocation targets, 0 unresolved calls. All semantic path fields were repository-relative and normalized; no local `.nuget` path or test SDK generated source path appeared in either report.
