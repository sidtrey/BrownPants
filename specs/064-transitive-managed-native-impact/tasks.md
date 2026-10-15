# Tasks: Transitive Managed-to-Native Impact

**Input**: Design documents in `specs/064-transitive-managed-native-impact/`

## Phase 1: User Story 1 - Follow managed changes across native boundaries (P1)

**Independent test**: Brownfield Bug Lab repository change traces through the impacted `ImportBatch` declaration to its bridge, native caller, and MFC dispatch; unrelated declarations are excluded.

- [X] T001 [US1] Add regression tests in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` for exact declaration seeding, separate managed/native depths, same-file unrelated declarations, and deterministic shortest-depth deduplication; verify they fail before implementation.
- [X] T002 [US1] Extend `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs` to map impacted managed symbols to exact declarations and seed native tracing only from matching relation target locations, preserving native `Depth` and direct path behavior.
- [X] T003 [US1] Add optional `ManagedImpactDepth` to native impact output and render it distinctly from native depth in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs` and `src/BrownPants.Cli/Output/CallImpactReportRenderer.cs`.

## Phase 2: User Story 2 - Preserve direct and focused impact behavior (P2)

**Independent test**: Existing direct workflow impact remains stable, and changed-repository semantic evidence includes exactly the connected native candidates.

- [X] T004 [US2] Add CLI evidence integration coverage in `tests/BrownPants.Tests/EvidenceCommandTests.cs` for transitive managed-to-native focus selection and exclusion of unrelated candidates.
- [X] T005 [US2] Update `README.md` call-impact documentation and add observed Bug Lab JSON/Markdown output and evidence limits to `specs/064-transitive-managed-native-impact/verification.md`.
- [X] T006 [US2] Run focused and full Release tests/build, Bug Lab impact/evidence commands, acceptance hash and evidence-ledger checks; record actual outputs in `specs/064-transitive-managed-native-impact/verification.md`.

## Dependencies and Order

- T001 establishes regression evidence before implementation.
- T002 and T003 depend on T001; T003 changes serialization/rendering for the new managed seed depth.
- T004 depends on the analyzer behavior in T002 and output contract in T003.
- T005 and T006 follow implementation and observed verification.
- No task is independently parallelizable because all implementation and evidence tasks build on the same impact model.
