# Tasks: Managed and Native Interop Analysis

**Input**: Design documents in `specs/001-managed-native-interop/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Regression coverage is included for the newly added VB.NET signals, evidence kinds, and false-positive guard. Each story lists its independent acceptance check for review.

**Organization**: Tasks are grouped by independently reviewable user story. Existing implementation is credited in the Notes; the tasks describe remaining work against the specification.

## Phase 1: Setup

**Purpose**: Existing .NET 10 CLI and xUnit projects are already initialized; no setup changes are needed.

## Phase 2: Foundational

**Purpose**: Establish explicit evidence semantics shared by all story findings.

- [X] T001 Add an evidence-kind field to architecture findings in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`, distinguishing source-local candidate signals from Roslyn-resolved call edges and project metadata.
- [X] T002 Render the evidence kind consistently in Markdown and JSON documentation in `src/BrownPants.Cli/Output/MarkdownReport.cs` and `specs/001-managed-native-interop/contracts/inspect-report.md`.

## Phase 3: User Story 1 - Trace managed-to-native boundaries (Priority: P1)

**Goal**: Help developers inspect managed/native boundary signals in C#, VB.NET, and native project/source evidence.

**Independent Test**: Inspect a mixed-language fixture or repository and confirm boundary categories include repository-relative locations and state whether they are candidate or resolved evidence.

- [X] T003 [US1] Parse VB.NET interop attributes and COM activation syntax into source-located boundary signals in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T004 [US1] Classify native MFC, COM, and C++/CLI matches as lexical candidate evidence and preserve the current source-location behavior in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T005 [US1] Update managed/native boundary guidance and evidence limits in `integrations/speckit/commands/context.md`.

## Phase 4: User Story 2 - Find construction and dependency wiring (Priority: P2)

**Goal**: Locate factory and DI registration/resolution patterns without treating names alone as proven runtime relationships.

**Independent Test**: Inspect source with representative managed and native factory/DI patterns and confirm each result is categorized, source-located, and labeled as candidate evidence.

- [X] T006 [US2] Add syntax-aware C# DI resolution and common factory invocation signals, retaining evidence snippets and avoiding comments or strings, in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T007 [US2] Add VB.NET factory and common DI registration/resolution syntax signals with source paths and lines in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T008 [US2] Describe supported factory and DI signals and their limits in `README.md` and `integrations/speckit/commands/context.md`.
- [X] T009 [US2] Add temporary-repository xUnit coverage for VB.NET factory, DI, COM, interop, candidate JSON evidence, ordinary `Create*` APIs, and multiline C++ raw-string false positives in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

## Phase 5: User Story 3 - Review older projects without a successful build (Priority: P3)

**Goal**: Keep readable inventory useful when legacy projects cannot be loaded or built.

**Independent Test**: Inspect a repository with a malformed project and readable managed/native files; verify static findings remain available and load/read limitations are reported.

- [X] T010 [US3] Ensure new language-specific pattern passes isolate file parse/read failures so remaining findings are retained in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T011 [US3] Clarify static versus `--semantic` evidence and partial project-load behavior in `README.md` and `specs/001-managed-native-interop/quickstart.md`.

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Check report consistency and Spec Kit workflow documentation.

- [X] T012 [P] Align report examples and JSON field descriptions with evidence semantics in `specs/001-managed-native-interop/contracts/inspect-report.md`.
- [X] T013 Confirm Codex CLI command names and BrownPants context-extension workflow in `README.md` and `integrations/speckit/README.md`.

## Dependencies and Execution Order

- Foundational tasks T001-T002 precede all story work because reports need an explicit evidence distinction.
- User Story 1 can proceed after foundation. User Stories 2 and 3 also depend only on foundation and can be implemented independently after that.
- Polish tasks follow the story changes; T011 and T012 touch separate documentation files and can proceed in parallel.

## Parallel Opportunities

- After T001-T002, US1 boundary work and US2 factory/DI work may proceed independently, though they share `ArchitecturePatternAnalyzer.cs` and should be sequenced if performed by one contributor.
- T005 can proceed alongside analyzer work. T008 documentation can proceed alongside US2 once signal names are agreed.
- T011 and T012 can proceed in parallel because they update separate files.

## Implementation Strategy

Deliver the evidence semantics and managed/native boundary findings first (US1, MVP). Add factory/DI coverage next (US2), then harden partial-input behavior and the Codex/Spec Kit quickstart (US3). Review each story against its independent acceptance check; use the repository's existing fixtures and `dotnet test BrownPants.sln` when the contributor requests verification.

## Existing Coverage

The repository already has C# Roslyn syntax analysis, VB.NET structural and architecture-pattern analysis, opt-in C#/VB semantic calls, `.vcxproj`/`.vcproj` MFC/CLR metadata, managed interop/factory/DI signals, native MFC/COM/C++/CLI lexical signals, deterministic Markdown/JSON reporting, and fixture coverage. The feature tasks extend these capabilities rather than recreating them.


## Phase 7: Convergence

**Purpose**: Close report-parity and verification gaps found when comparing the current implementation with the feature requirements.

- [X] T014 Remove the Markdown-only 50-signal cap so architecture findings emitted in Markdown match JSON's configured sample limit per FR-008 (partial).
- [X] T015 Extend the repeated-scan stability fixture to compare serialized JSON output as well as Markdown per SC-004 (partial).

## Phase 8: Convergence

**Purpose**: Complete Markdown/JSON parity for all report sections, not only architecture signals.

- [X] T016 Remove Markdown-only section caps and include VB invocation samples so both formats expose the same available report data per FR-008 (partial).

## Phase 9: Dependency Construction Coverage

**Purpose**: Surface constructor injection and explicit factory delegates in managed code.

- [X] T017 Detect constructor parameters that indicate injected dependencies or `Func<T>` factories in C# and VB.NET in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T018 Add fixture coverage for C# and VB.NET constructor injection, factory delegates, and ordinary-value constructor parameters in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

## Phase 10: COM Interop Evidence

**Purpose**: Cover managed activation and marshalling paths used by clients that bridge C# or VB.NET to COM/MFC components.

- [X] T019 Add C# and VB.NET fixture assertions for CLSID/active-object activation, `Marshal` interface conversion and object wrapping, and COM metadata attributes.
- [X] T020 Document COM activation, marshalling, and interface metadata candidate coverage and its syntax-based evidence limits.

## Phase 11: Factory Candidate Precision

**Purpose**: Avoid classifying void-like methods named `Create` as factory method declarations.

- [X] T021 Require C# factory method candidates to return a value and VB.NET candidates to be `Function` declarations; cover positive and negative examples in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
