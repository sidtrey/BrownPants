# Tasks: Indirect Placeholder Accessors

## Phase 1: Analysis and evidence model

- [X] T001 [US1] Add fixture coverage for property and method wrappers, non-throwing and multi-hop exclusions, determinism, limits, and Markdown/JSON parity in `tests/BrownPants.Tests/CSharpImplementationPlaceholderAnalyzerTests.cs`.
- [X] T002 [US1] Detect one-hop same-type wrappers around direct NotImplementedException factories and throws in `src/BrownPants.Cli/Analysis/CSharpImplementationPlaceholderAnalyzer.cs`.
- [X] T003 [US1] Render indirect candidate evidence and accurate candidate member counts in `src/BrownPants.Cli/Output/MarkdownReport.cs` while retaining JSON compatibility.

## Phase 2: Tests and cross-repository proof

- [X] T004 [US1] Build and test BrownPants, run parser-backed FullMetalFile proof scan, and record acceptance evidence in `specs/081-indirect-placeholder-accessors/quickstart.md` and `acceptance-evidence.json`.
- [X] T005 [US1] Reconcile the stale five-accessor live claim with current FullMetalFile source evidence, apply the one-maintainer waiver to SC-003, and refresh the acceptance baseline.

## Independent Test

Run `dotnet test BrownPants.sln -c Release --no-build --filter FullyQualifiedName~CSharpImplementationPlaceholderAnalyzerTests`, then inspect `FullMetalFile_React/FullMetalFile.Infrastructure` with `--format json --limit 10000`; confirm current delegated accessors appear as indirect candidates and constructor-initialized `IndexingSessions` is excluded.
