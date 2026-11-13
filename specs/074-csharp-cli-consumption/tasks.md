# Tasks: C# Consumption of C++/CLI Libraries

**Input**: Design documents in `specs/074-csharp-cli-consumption/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/managed-native-evidence.md`, `quickstart.md`

## Phase 1: Setup

**Purpose**: Establish a source-only fixture that mirrors the pinned legacy consumer pattern.

- [x] T001 Add a minimal C# consumer, legacy `.csproj` reference/`HintPath`, CLR-enabled `.vcxproj`, C++/CLI wrapper, and qualified native target under `examples/brownfield-bug-lab/legacy/ManagedConsumer/`.
- [x] T002 Record fixture provenance, expected paths, and deliberately absent build/runtime evidence in `examples/brownfield-bug-lab/legacy/ManagedConsumer/README.md`.

## Phase 2: Foundational

**Purpose**: Extend bounded source/project evidence models before adding consumer-to-wrapper matching.

- [x] T003 Extend `DotNetProjectInfo` and `DotNetProjectAnalyzer` in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and `src/BrownPants.Cli/Analysis/DotNetProjectAnalyzer.cs` to retain static `Reference Include`, `HintPath`, and `ProjectReference` facts with safe XML parsing and normalized paths.
- [x] T004 Extend `NativeProjectInfo` and `NativeProjectAnalyzer` in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and `src/BrownPants.Cli/Analysis/NativeProjectAnalyzer.cs` to retain target/output assembly identity for discovered CLR-enabled projects, preserving unknown and conditional metadata.
- [x] T005 Add foundational project metadata tests in `tests/BrownPants.Tests/RepositoryScannerTests.cs` for old-style references, project references, malformed XML, and paths outside the scanned root.

**Checkpoint**: Static project references and source project identities are available without evaluating MSBuild.

## Phase 3: User Story 1 - Follow a C# call into a mixed-mode library (Priority: P1)

**Goal**: Start at a C# call site and follow a referenced C++/CLI wrapper to supported native calls.

**Independent Test**: On the fixture from T001, JSON and Markdown contain the consumer call, reference evidence, wrapper candidate, and qualified native call with correct repository-relative paths and one-based lines.

### Tests for User Story 1

- [x] T006 [P] [US1] Add failing C# consumer call-site and receiver-type tests in `tests/BrownPants.Tests/RepositoryScannerTests.cs` for fully qualified explicit local types and local object creation.
- [x] T007 [P] [US1] Add failing end-to-end JSON and Markdown evidence tests in `tests/BrownPants.Tests/EvidenceCommandTests.cs` for the C# -> wrapper -> native candidate path.
- [x] T008 [P] [US1] Add failing changed-consumer impact tests in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` for outgoing wrapper and native edges.

### Implementation for User Story 1

- [x] T009 [US1] Extend bounded invocation indexing with containing method, receiver expression/type evidence, source column, and supported local type facts in `src/BrownPants.Cli/Analysis/CSharpAnalyzer.cs` and `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [x] T010 [US1] Identify managed wrapper types/methods in CLR-enabled projects and match C# call sites using the receiver, method, and project reference evidence in `src/BrownPants.Cli/Analysis/ManagedNativeConsumerAnalyzer.cs` and `src/BrownPants.Cli/Analysis/RepositoryScanner.cs`.
- [x] T011 [US1] Add a distinct `csharp-cppcli-call` candidate relation and stable source/reference match reasons in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [x] T012 [US1] Include managed-consumer candidates in full inventory and focused evidence with bounded sampling in `src/BrownPants.Cli/Analysis/EvidenceReport.cs` and `src/BrownPants.Cli/Analysis/EvidenceFocusAnalyzer.cs`.
- [x] T013 [US1] Extend changed-path impact to follow a changed C# consumer outward through the wrapper and supported native calls in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`.
- [x] T014 [US1] Render managed-to-native candidate paths, source locations, and evidence limits distinctly in `src/BrownPants.Cli/Output/MarkdownReport.cs`.

**Checkpoint**: The pinned source shape is traceable from C# while existing managed-to-native and native-to-managed paths remain separate.

## Phase 4: User Story 2 - Understand incomplete or uncertain interop paths (Priority: P2)

**Goal**: Preserve ambiguity, unresolved references, unsupported call forms, and safety boundaries.

**Independent Test**: Decoy and ambiguous fixtures never produce a false unique relation; unresolved/omitted causes appear in JSON, Markdown, and impact completeness.

### Tests for User Story 2

- [x] T015 [P] [US2] Add failing ambiguity, unresolved target, unreferenced same-name method, comments/strings, and unsupported receiver tests in `tests/BrownPants.Tests/RepositoryScannerTests.cs` and `tests/BrownPants.Tests/EvidenceCommandTests.cs`.
- [x] T016 [P] [US2] Add failing deterministic output, sample-bound, and incomplete-impact tests in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`.

### Implementation for User Story 2

- [x] T017 [US2] Keep all candidate wrapper locations and distinguish `candidate`, `ambiguous`, `unresolved`, and `unsupported` outcomes in `src/BrownPants.Cli/Analysis/NativeFlowAnalyzer.cs`.
- [x] T018 [US2] Constrain normalized reference paths to discovered in-root files and report unsafe/external paths without reading them in `src/BrownPants.Cli/Analysis/DotNetProjectAnalyzer.cs` and `src/BrownPants.Cli/Analysis/NativeProjectAnalyzer.cs`.
- [x] T019 [US2] Propagate source/reference/index omissions into evidence limitations and impact completeness in `src/BrownPants.Cli/Analysis/EvidenceFocusAnalyzer.cs` and `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`.

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Document the candidate contract and prove readiness across supported entry points.

- [x] T020 Update `README.md`, `examples/brownfield-bug-lab/README.md`, and `specs/074-csharp-cli-consumption/quickstart.md` with C#-first commands, representative report output, and candidate-only limitations.
- [x] T021 Add or update `tests/PackedManagedInteropSmoke.ps1` to verify packaged inspect, focused evidence, and changed-consumer impact output on the new fixture.
- [x] T022 Run `dotnet build BrownPants.sln -c Release`, `dotnet test BrownPants.sln -c Release --no-build`, the managed interop package smoke, acceptance hash, and acceptance evidence validation; record exact observed results in `specs/074-csharp-cli-consumption/verification.md`.
- [x] T023 Complete `specs/074-csharp-cli-consumption/acceptance-evidence.json` with criterion mappings, concrete commands, and existing evidence paths for each completed task.
- [x] T024 Review generated JSON/Markdown and the fixture diff for deterministic ordering, unsafe path reads, accidental project evaluation, and claims stronger than source evidence; record the review in `specs/074-csharp-cli-consumption/verification.md`.

## Dependencies & Execution Order

- T001-T002 establish the independent source-only fixture.
- T003-T005 establish static project metadata and tests before relation implementation.
- US1 tasks T006-T014 deliver the MVP; run its focused tests at the checkpoint.
- US2 tasks T015-T019 harden ambiguity, safety, bounds, and completeness on top of US1.
- T020-T024 are required before calling the feature ready or pushing it for review.

## Parallel Opportunities

- T006-T008 can be authored independently after the fixture exists.
- T015-T016 can be authored independently after US1 behavior is established.
- T020 documentation can proceed after JSON/Markdown field names are stable.

## Implementation Strategy

Deliver US1 first as the MVP: a referenced C# consumer call must trace to one source-backed C++/CLI candidate and onward to a supported native call, with candidate labels and locations. Then complete US2 so uncertainty and omissions cannot be mistaken for proof. Finish with package smoke, full verification, docs, and the evidence ledger before publishing the branch.
