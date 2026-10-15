# Tasks: WinForms Cross-Thread UI Access Candidates

**Input**: Design documents in `/specs/040-winforms-thread-safety/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/winforms-cli.md`, and `quickstart.md`.

**Tests**: Required by AC-001–AC-008 and SC-001–SC-005. Use temporary C#/VB repositories; analysis must be source-only and OS-independent.

## Phase 1: Control property assignment evidence

**Independent Test**: Parse fixtures containing direct built-in Controls, custom source-derived Controls, ordinary objects, comments, strings, and C#/VB assignments; verify only source-recognized control property writes are reported with stable source locations.

- [x] T001 [US1] Add C# and Visual Basic fixture tests for Control property assignments, source-derived controls, ordinary object exclusions, comments/strings, relative locations, and deterministic ordering in `tests/BrownPants.Tests/WinFormsUiThreadingTests.cs`.
- [x] T002 [US1] Implement bounded C#/VB parsing, WinForms import/type recognition, source-local Control inheritance/member maps, property assignment extraction, parser diagnostics, and finding records in `src/BrownPants.Cli/Analysis/WinFormsUiThreadingAnalyzer.cs` and `WinFormsUiThreadingModels.cs`; reuse C# parse-version selection without MSBuild evaluation.

## Phase 2: Execution-context and marshal evidence

**Independent Test**: Fixture assignments in `Task.Run`, `ThreadPool`, `Thread`, and `BackgroundWorker.DoWork` shapes plus `Invoke`, `BeginInvoke`, `InvokeAsync`, `SynchronizationContext.Post/Send`, and `InvokeRequired` markers; assert only observed syntax classifications are emitted.

- [x] T003 [US1] Extend C#/VB fixture tests to cover recognized background callback evidence, unknown execution context, direct dispatcher callback containment, weak `InvokeRequired` markers, combined background/marshal evidence, and no safe/defect conclusion in `tests/BrownPants.Tests/WinFormsUiThreadingTests.cs`.
- [x] T004 [US1] Add syntax-local callback and marshaling classifiers to `WinFormsUiThreadingAnalyzer.cs`; distinguish nested dispatcher callbacks from marker-only references, keep unknown context explicit, and label every result `Candidate`.

## Phase 3: CLI, reports, and release checks

- [x] T005 [US2] Add `brownpants winforms analyze <repository> [--format markdown|json] [--limit 1..10000]`, stable Markdown/JSON rendering, no-source-excerpt guarantees, and command/help dispatch in `src/BrownPants.Cli/Analysis/WinFormsCommand.cs`, `src/BrownPants.Cli/Output/WinFormsUiThreadingReportRenderer.cs`, and `src/BrownPants.Cli/Program.cs`; test format equivalence, bounds, path safety, no writes, and error handling in `WinFormsUiThreadingTests.cs`.
- [x] T006 [US2] Document evidence labels, recognized patterns, .NET Framework and cross-host usage, and limitations in `integrations/winforms/README.md` and `README.md`; add a source-only packed CLI smoke fixture in `tests/PackedWinFormsSmoke.ps1` and invoke it from `.github/workflows/ci.yml`.
- [x] T007 [US1, US2] Verify focused tests, Release build/full test suite, acceptance hash/evidence, package audit, BrownPants-on-BrownPants inventory, and packed smoke; record observed results in `specs/040-winforms-thread-safety/verification.md`.

## Dependencies

- T001 precedes T002.
- T003 follows T002 and precedes T004.
- T005 follows the analyzer contract; T006 follows CLI naming/output finalization.
- T007 is the completion gate. Each task maps to `AC-###`/`SC-###` in `acceptance-evidence.json` before implementation.
