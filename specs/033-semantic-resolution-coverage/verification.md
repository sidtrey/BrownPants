# Verification: Semantic Resolution Coverage

## Acceptance criteria

- Acceptance criteria SHA-256 remains `87b14ef3fcac2d6d5769bb550086cd0f7f6ac83b3409326ff6e11e18ae1dffab`.
- Evidence ledger check passed: 6 tasks and 11 criteria mapped.
- T001–T006 completed; focused semantic tests cover count reconciliation, skipped `nameof`, repository target locations, compiler diagnostics, standalone projects, duplicate solution membership, issue totals, and bounded samples.

## Build and tests

- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --filter-class BrownPants.Tests.RepositoryScannerTests` — passed, 71 tests.
- `dotnet test BrownPants.sln -c Release` — passed, 163 tests.
- `dotnet run --project src/BrownPants.Cli -c Release --no-build -- inspect --help` — passed; help distinguishes syntax-only mode and explains semantic coverage and MSBuild execution risk.

## Self-inspection

Release CLI semantic inspection passed in JSON and Markdown. The JSON report found 2/2 projects loaded, 0 failed, complete coverage, and 0 workspace issues or compiler errors. Invocation totals reconciled: 5,708 syntax nodes = 17 skipped + 5,691 analyzed; 5,691 analyzed = 5,691 resolved + 0 unresolved. Markdown displayed repository target paths and lines. Generated reports are ignored under `TestResults/semantic-self-feature-033.*`.

## Limits

Semantic evidence reflects the selected stable solution context and the projects MSBuild returns. Workspace issues mark coverage incomplete; compiler errors remain diagnostics rather than proof of runtime defects. One solution is selected to avoid duplicate solution evaluation; discovered projects outside it are opened directly.
