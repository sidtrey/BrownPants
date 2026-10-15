# Verification: Impact Graph Coverage

**Date:** 2026-10-15

## Automated checks

- `dotnet build BrownPants.sln -c Release` — passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 274 tests.
- Focused `CallImpactAnalyzerTests` and `RepositoryScannerTests` — passed, 94 tests. The regression test also passed alone.
- Packed `BrownPants.Cli` 0.2.1 installed from a local package source using CI's NuGet signature-validation configuration; `PackedImpactGraphCoverageSmoke.ps1` passed. It verified the public one-edge sample, found the later changed-method caller, and confirmed internal graph fields are absent from JSON.
- `SpeckitExtensionTests.ps1` passed with the required live Spec Kit CLI check; `OpenSpecInstallerTests.ps1` passed.
- The acceptance criteria SHA-256 matched `0cc52c140daebe0e7b778c3469ac8bad601c012d4be674c8c3219c4592c8a742`; the evidence ledger validated 7 tasks against 11 criteria.

## BrownPants-on-BrownPants evidence

At the repository root, semantic inspection found 752 files, loaded 5 of 5 managed projects, and counted 10,035 resolved invocations plus 1,095 resolved constructors (11,130 total). The public semantic sample remained at 10,000 edges, retained 821 declarations, and exposed no `ImpactGraph*` JSON properties.

Impact for `examples/brownfield-bug-lab/managed/Workflows/OrderImportWorkflow.cs` returned 10 managed findings, summarized as 4 direct callers and 1 transitive caller, plus 3 native/interop candidate links. The report correctly remained incomplete because 4 invocations were unresolved and the workspace reported 1 issue. These are independent limitations; public call-sample truncation was not reported as impact-graph truncation.

## Scope limits

Local verification ran on Windows only; the CI workflow retains Windows, Ubuntu, and macOS coverage. The packed smoke uses a small controlled SDK-style fixture. This verifies CLI packaging and sample-order behavior, while the BrownPants self-run provides the larger repository evidence above. The 100,000-edge production ceiling was verified with an injected one-edge test ceiling, not by constructing a 100,000-edge fixture.
