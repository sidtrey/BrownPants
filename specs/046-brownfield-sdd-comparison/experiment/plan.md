# Implementation Plan: Restore Test-Directory Path Classification

## Summary

Repair the existing path heuristic so an exact `test` or `tests` directory component classifies nested fixture files as test candidates, without matching `test` inside unrelated names. Preserve current filename-based rules.

## Codebase Evidence

- `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` owns `IsTestPath` and is called by repository inventory and investigation code.
- `tests/BrownPants.Tests/RepositoryScannerTests.cs` contains the scanner fixture tests and is the required regression-test location.
- The existing scanner already contains the correct exact path-component rule in the clean source. The benchmark seed removes this branch in both copies to create the same reproducible defect.
- Default `inspect` supplies deterministic repository structure and C#/VB syntax evidence. Use `impact` only with a source file if semantic project context can be loaded; report unresolved or incomplete evidence honestly.

## Technical Context

- .NET 10 CLI and xUnit.
- Keep the fix local to test path classification; do not change generic discovery exclusions.
- Preserve case-insensitive exact component and filename behavior.
- Focused validation: `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~RepositoryScannerTests`.
- Full validation: `dotnet test BrownPants.sln -c Release --no-restore`.

## Design

Restore exact component checks in `RepositoryScanner.IsTestPath`, then add explicit positive and negative examples in `RepositoryScannerTests`. Avoid substring matching. Run tests with the same commands in each comparison condition.

## Risks

- Platform path separators can differ; normalize before splitting and keep fixture paths repository-relative.
- Existing filename rules may mask the directory-specific failure; use a fixture filename that is not itself a test name.
- Passing tests do not prove all consumers interpret a candidate as actual test coverage.
