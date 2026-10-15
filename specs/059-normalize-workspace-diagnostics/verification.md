# Verification: Normalize Workspace Diagnostics

## Automated checks

- `dotnet test BrownPants.sln -c Release --filter "FullyQualifiedName~WorkspaceDiagnostic|FullyQualifiedName~SemanticAnalyzer_LoadsUnlistedProjectsOnceAndReportsFailedProjectsAndIssueTotals"` — passed, 3 tests.
- `dotnet test BrownPants.sln -c Release --filter "FullyQualifiedName~WorkspaceDiagnosticNormalizer"` — passed, 3 tests, including URL preservation.
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 277 tests.
- Acceptance criteria hash — passed: `e618546975d0786c1c577398a0d99c23889b407987f97287c15426cf7ba82aeb`.
- Acceptance evidence ledger — passed: 4 tasks mapped to 8 criteria.

## BrownPants-on-BrownPants

Command: `dotnet run --project src/BrownPants.Cli -- inspect . --format json --semantic --limit 10000`.

The scan reported 775 files, discovered and loaded 5 projects, 10,096 resolved and 4 unresolved invocations, 1 workspace issue, and 21 compiler errors. The workspace issue sample was not truncated, and none of its text contained the absolute checkout root. The original explanation remains visible:

> Msbuild failed when processing the file 'examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj' with message: Project does not contain 'Compile' target.

Coverage correctly remains incomplete because semantic resolution still has unresolved calls and compiler/workspace diagnostics. This change does not claim to fix those underlying project-evaluation conditions.
