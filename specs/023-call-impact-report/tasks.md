# Tasks: Call-Trace Change Impact Report

**Input**: Design documents in `specs/023-call-impact-report/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/impact-report.md`
**Tests**: Fixture-based xUnit coverage is required by the feature plan and project constitution.

## Phase 1: Setup

**Purpose**: Confirm feature scope and preserve acceptance traceability.

- [X] T001 Verify the frozen acceptance digest with `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/023-call-impact-report/spec.md`
- [X] T002 Create `specs/023-call-impact-report/acceptance-evidence.json` mapping all tasks to AC/SC IDs and planned verification commands

## Phase 2: Foundational

**Purpose**: Extend resolved semantic call evidence so changed declarations can be mapped without fuzzy names.

- [X] T003 [P] [US1] Add target declaration path and line to resolved C#/VB call-edge models in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and populate locations in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`
- [X] T004 [US1] Add regression fixtures for target declaration mapping, overloads, and missing source locations in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`

## Phase 3: User Story 1 - Assess Change Impact (Priority: P1)

**Goal**: Report direct and transitive resolved callers and evidence-limited QA/complexity guidance for changed source paths.

**Independent Test**: Run xUnit fixtures over a temporary multi-project C#/VB repository and verify shortest caller depth, test-candidate labels, completeness issues, deterministic output, and graph metrics.

### Tests for User Story 1

- [X] T005 [US1] Add call-impact traversal fixtures for direct/transitive callers, cycles, deduplication, multiple changed paths, and shortest depth in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`
- [X] T006 [US1] Add completeness and boundary fixtures for unmapped/unsupported paths, partial loads, unresolved edges, and truncated samples in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`
- [X] T007 [US1] Add Markdown/JSON parity and deterministic ordering fixtures in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`

### Implementation for User Story 1

- [X] T008 [US1] Implement reverse breadth-first call traversal, shortest-depth deduplication, test-candidate classification, graph metrics, and completeness diagnostics in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`
- [X] T009 [US1] Implement a separately versioned impact report model and stable Markdown/JSON rendering in `src/BrownPants.Cli/Output/CallImpactReportRenderer.cs`
- [X] T010 [US1] Add the explicit `impact` command, changed-path validation, semantic graph loading, format and limit options in `src/BrownPants.Cli/Program.cs`
- [X] T011 [US1] Run focused and full xUnit suites, release build, acceptance hash/evidence checks, and BrownPants self-inspection; record observed outputs in `specs/023-call-impact-report/verification.md`

## Dependencies & Execution Order

- T001–T002 precede source work.
- T003–T004 establish declaration mapping and its regression coverage.
- T005–T007 define observable behavior before analyzer/rendering integration.
- T008–T010 depend on the semantic edge model and tests.
- T011 depends on all implementation tasks.
- User Story 1 is the MVP and only story in this feature.

## Parallel Opportunities

T003 and T004 touch different files. T005, T006, and T007 can be developed independently before integration, subject to shared test fixture conventions.

## Implementation Strategy

Complete declaration mapping first, then prove traversal, limitation handling, and rendering with fixtures. Wire the CLI after the report components work. Finish with the prescribed build, tests, BrownPants before/after inspection, and evidence ledger reconciliation.

## Notes

- Constructor/object-creation edges are outside the frozen method-invocation acceptance contract.
- Observed graph counts are not effort estimates; test paths are candidates, not coverage proof.
- The command is explicit opt-in because MSBuildWorkspace may evaluate imports and design-time targets.
