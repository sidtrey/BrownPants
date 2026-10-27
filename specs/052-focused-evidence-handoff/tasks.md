# Tasks: Focused Evidence Handoff

**Input**: Design documents in `specs/052-focused-evidence-handoff/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/focused-evidence.json.md`

## Phase 1 - Regression fixtures and evidence ledger

- [x] T001 [US1] Add temporary mixed-repository focused evidence regression cases in `tests/BrownPants.Tests/EvidenceCommandTests.cs` and map them to the frozen criteria in `specs/052-focused-evidence-handoff/acceptance-evidence.json`.

## Phase 2 - Focused evidence selection

- [x] T002 [US1] Add focus request/result records and optional JSON fields without changing unfiltered serialization in `src/BrownPants.Cli/Analysis/EvidenceReport.cs`.
- [x] T003 [US1] Implement clue, changed-path, impact, and test-path selection with deterministic deduplication and counts in `src/BrownPants.Cli/Analysis/EvidenceFocusAnalyzer.cs`.
- [x] T004 [US1] Parse repeatable `--clue` and `--changed` options, enforce selector validation, invoke semantic impact only when explicitly requested, and update help in `src/BrownPants.Cli/EvidenceCommand.cs`.
- [x] T005 [US1] Verify no-focus compatibility, clue and changed-path behavior, error cases, deterministic ordering, and count reconciliation with `dotnet test BrownPants.sln --filter FullyQualifiedName~EvidenceCommandTests`.

## Phase 3 - Brownfield usage and verification

- [x] T006 [US1] Document focused and full handoff examples, evidence selection meanings, semantic-mode caution, and limits in `README.md`, `examples/brownfield-bug-lab/README.md`, and `specs/052-focused-evidence-handoff/quickstart.md`.
- [x] T007 [US1] Run the focused-output Brownfield Bug Lab check, verify each related answer-key fact and reduced finding count, update acceptance evidence, verify the criteria hash and ledger, and run `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, and `git diff --check`.

## Dependencies and verification

- T001 precedes T002-T004 so tests define observed contract behavior.
- T002 and T003 precede T004; T004 integrates the focused selection into the CLI.
- T005 follows T001-T004; T006-T007 follow implementation and focused verification.
- All acceptance scenarios are tested in one user story: full-mode compatibility, clue selection, changed-path/caller selection, mixed selectors, validation failures, limits, and deterministic JSON.

## Phase 4: Convergence

- [x] T008 [US1] Include a small, bounded sample of literal clue path/line matches and an omitted-match count in each focused clue summary, so developers can navigate matches even when candidate findings are absent or upstream evidence was sampled; preserve unfiltered output and cover path/content clues and limits in `src/BrownPants.Cli/Analysis/EvidenceReport.cs`, `src/BrownPants.Cli/Analysis/EvidenceFocusAnalyzer.cs`, and `tests/BrownPants.Tests/EvidenceCommandTests.cs`; document the additive contract in `specs/052-focused-evidence-handoff/contracts/focused-evidence.json.md` and `quickstart.md`.
