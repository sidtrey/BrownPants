# Tasks: Scope-Aware WinForms Control Evidence

**Input**: Design documents in `specs/053-scope-aware-winforms-evidence/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/winforms-report.md`, `quickstart.md`

## Phase 1: Regression fixtures (User Story 1)

**Independent test**: The frozen C#/VB fixture answer key contains all control writes and excludes same-named ordinary object writes across scopes and files.

- [x] T001 [US1] Add C# and Visual Basic multi-file fixtures for partial/derived controls, aliases, typed members, nested shadowed locals, chained members, and ordinary-object decoys in `tests/BrownPants.Tests/WinFormsUiThreadingTests.cs`.
- [x] T002 [US1] Add bounded ambiguity, malformed source, deterministic output, and read-only repository checks to `tests/BrownPants.Tests/WinFormsUiThreadingTests.cs`.

## Phase 2: Repository-wide source index

- [x] T003 [US1] Introduce per-file parse context and namespace-qualified source type/member indexes for C# and VB in `src/BrownPants.Cli/Analysis/WinFormsUiThreadingAnalyzer.cs` and `src/BrownPants.Cli/Analysis/WinFormsUiThreadingModels.cs`.
- [x] T004 [US1] Resolve partial and derived Control chains plus aliases and declared member types across indexed files in `src/BrownPants.Cli/Analysis/WinFormsUiThreadingAnalyzer.cs`.

## Phase 3: Scope-aware receiver classification

- [x] T005 [US1] Replace flat C#/VB identifier maps with nearest-scope local/parameter/member lookup and typed member-chain traversal in `src/BrownPants.Cli/Analysis/WinFormsUiThreadingAnalyzer.cs`.
- [x] T006 [US1] Add unresolved receiver count, stable bounded issue records, and indexed type coverage to `src/BrownPants.Cli/Analysis/WinFormsUiThreadingModels.cs` and `src/BrownPants.Cli/Output/WinFormsUiThreadingReportRenderer.cs`.
- [x] T007 [US1] Verify CLI compatibility, deterministic counts, parser error visibility, and source-only behavior with `dotnet test BrownPants.sln --filter FullyQualifiedName~WinFormsUiThreadingTests`.

## Phase 4: Brownfield verification and documentation

- [x] T008 [US1] Update WinForms command and integration docs with cross-file/scoped evidence and unresolved coverage limits in `src/BrownPants.Cli/Analysis/WinFormsCommand.cs`, `integrations/winforms/README.md`, and `README.md`.
- [x] T009 [US1] Run the analyzer on the Brownfield Bug Lab and BrownPants, record packed CLI smoke and self-analysis evidence in `specs/053-scope-aware-winforms-evidence/verification.md`, then run `dotnet build BrownPants.sln -c Release --no-restore` and `dotnet test BrownPants.sln -c Release --no-restore`.

## Dependencies and execution order

T001-T002 define regression behavior and should fail before T003-T006. T003-T004 build the shared source index. T005-T006 consume it and shape output. T007 verifies the complete story; T008-T009 complete documentation and brownfield verification.
