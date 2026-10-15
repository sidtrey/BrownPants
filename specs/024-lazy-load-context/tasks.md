# Tasks: On-Demand Instruction Sections

**Input**: Design documents from `specs/024-lazy-load-context/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/instruction-section.md`

**Tests**: Fixture-based xUnit tests are required by the feature workflow and repository constitution.

## Phase 1: User Story 1 - Retrieve a selected instruction section (Priority: P1) 🎯 MVP

**Goal**: Return one auditable section selected by source path and anchor.

**Independent Test**: A temporary repository with nested headings, duplicate headings, and fenced heading examples returns only the selected section, with stable path, anchor, and line range.

### Tests for User Story 1

- [x] T001 [P] [US1] Add failing xUnit fixtures for section boundaries, nested headings, duplicate anchors, fenced-code headings, Markdown/JSON parity, and line ranges in `tests/BrownPants.Tests/InstructionSourceAnalyzerTests.cs`

### Implementation for User Story 1

- [x] T002 [US1] Parse Markdown headings outside backtick/tilde fences while preserving anchor normalization and duplicate suffixes in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs`
- [x] T003 [US1] Store 1-based inclusive section line boundaries and extract the selected heading through the next same-or-higher level heading in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs`
- [x] T004 [US1] Add stable Markdown and JSON rendering for selected section metadata and content in `src/BrownPants.Cli/Output/InstructionSourceReportRenderer.cs`
- [x] T005 [US1] Add `--section` dispatch and enforce exactly one `--source` for section requests in `src/BrownPants.Cli/Program.cs`

**Checkpoint**: A valid source/anchor request returns only that section in both formats.

## Phase 2: User Story 2 - Handle unavailable or unsafe section requests (Priority: P1)

**Goal**: Refuse missing, unreadable, unsupported, or unsafe requests without returning unrelated content.

**Independent Test**: Temporary repositories prove missing sources/anchors, invalid formats, traversal paths, invalid UTF-8, size limits, and reparse-point paths produce failure and no section output.

### Tests for User Story 2

- [x] T006 [US2] Add failing xUnit cases for missing source/anchor, invalid arguments/format, traversal, unsupported encoding, size limits, and reparse-point paths in `tests/BrownPants.Tests/InstructionSourceAnalyzerTests.cs`

### Implementation for User Story 2

- [x] T007 [US2] Reuse recognized/explicit instruction-source discovery, repository containment, size, encoding, and reparse-point checks without full-file fallback in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs`
- [x] T008 [US2] Route invalid, missing, and unsafe section requests to concise stderr diagnostics with established exit codes in `src/BrownPants.Cli/Program.cs`

**Checkpoint**: Invalid requests fail without returning another section or full source.

## Phase 3: Polish & Cross-Cutting Concerns

- [x] T009 [P] Update CLI help, README examples, and all maintained Spec Kit context guidance with the retrieval example and explicit host-loading/token-savings limitation in `src/BrownPants.Cli/Program.cs`, `README.md`, `integrations/speckit/commands/context.md`, `.specify/extensions/brownpants/commands/context.md`, and `.agents/skills/speckit-brownpants-context/SKILL.md`
- [x] T010 Validate the documented Markdown and JSON quickstart scenarios in `specs/024-lazy-load-context/quickstart.md`
- [x] T011 Run `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, the acceptance digest check, and acceptance-evidence validation; record observed results in `specs/024-lazy-load-context/verification.md`

## Dependencies & Execution Order

- T001 and T006 are tests-first; they touch the same test file and run sequentially.
- T002-T005 implement US1 in sequence because the parser, output, and CLI share data.
- T006-T008 implement and validate US2 after the core lookup flow exists.
- T009-T011 complete guidance and repository verification.
- US2 depends on US1's source/anchor retrieval flow.

## Parallel Opportunities

No implementation tasks safely overlap because the analyzer, CLI, and test fixture file each have a single ordered change path. T009 can be reviewed after the CLI syntax is established.

## Implementation Strategy

Deliver US1 as the MVP and verify extraction independently. Then add US2 safety/error behavior. Finish with user guidance, build/tests, unchanged acceptance digest, and a complete evidence ledger. Retrieval does not prove that a host unloaded duplicate instructions or reduced token usage.
