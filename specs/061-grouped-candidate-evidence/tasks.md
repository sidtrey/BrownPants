# Tasks: Grouped Candidate Evidence

**Input**: Design documents in `specs/061-grouped-candidate-evidence/`

## Phase 1: User Story 1 - Review repeated candidate locations together (P1)

**Independent test**: Emit evidence from temporary repositories with repeated and distinct source signals. Verify deterministic exact-key groups link all emitted per-line IDs, raw findings do not change, and focused reports contain only selected members.

- [X] T001 [US1] Add failing fixture coverage for repeated candidate roll-ups, exact group keys, stable location order, and unchanged raw findings in `tests/BrownPants.Tests/EvidenceCommandTests.cs`.
- [X] T002 [US1] Add the repeated candidate group report model and deterministic grouping over emitted candidate findings in `src/BrownPants.Cli/Analysis/EvidenceReport.cs`.
- [X] T003 [US1] Recompute groups after clue/changed-path filtering and verify selected finding IDs and `matchedBy` reasons in `src/BrownPants.Cli/Analysis/EvidenceFocusAnalyzer.cs` and `tests/BrownPants.Tests/EvidenceCommandTests.cs`.
- [X] T004 [US1] Support grouped `lines` in the result schema/scorer, derive and hash a v4 source-grounded answer key without changing the task spec, add the v4 prompt, run evaluator self-check and both matched conditions, and record counts and token usage in `examples/brownfield-bug-lab/evaluation/` and `docs/comparisons/`.
- [X] T005 [US1] Update user documentation, run Release build/full tests and BrownPants self-analysis, validate the frozen criteria hash and evidence ledger, and record final results in `README.md` and `specs/061-grouped-candidate-evidence/verification.md`.

## Dependencies & Execution Order

- T001 defines observable report and compatibility behavior before implementation.
- T002 depends on T001 and implements the shared full-report grouping.
- T003 depends on T002 and applies the same grouping after focused selection.
- T004 depends on the group contract and verifies the measured handoff improvement while preserving v3 artifacts and the frozen bug task acceptance hash.
- T005 runs after implementation and comparison evidence are complete.

## Implementation Strategy

Preserve per-location findings as canonical evidence. Add a linked roll-up for repeated architecture/framework candidates, verify it remains correct after focus filtering, then teach the evaluation format to report one grouped finding with all reviewed lines. Publish the measured result even if the precision target fails.
