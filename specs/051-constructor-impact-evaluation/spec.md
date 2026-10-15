# Feature Specification: Constructor Impact Bug Benchmark

**Feature Branch**: `051-constructor-impact-evaluation`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Create a fair BrownPants versus vanilla SDD benchmark around a seeded brownfield bug involving generic constructor injection, factory creation, DI registration, and test callers. Record whether BrownPants improves the result without presenting a losing or inconclusive run as a win.

## User Scenarios & Testing

### User Story 1 - Compare BrownPants evidence on a constructor bug (Priority: P1)

A maintainer wants to see whether BrownPants helps an agent find the fix and affected callers when a generic service constructor silently ignores its injected repository. The comparison must use the same broken source, task, model, permissions, and test, with BrownPants evidence supplied only to the assisted condition.

**Why this priority**: Constructor-based factories and DI are common brownfield paths that earlier call-only tracing could miss. A controlled bug scenario can test this specific capability against ordinary repository inspection.

**Independent Test**: Verify the fixture passes before seeding, fails after seeding, produces identical broken copies, exposes the same task inputs to both conditions, and scores both returned findings against a hidden answer key.

**Acceptance Scenarios**:

1. **Given** the clean demo fixture, **when** its independent check runs, **then** it passes; after the constructor bug is seeded, the same check fails with the expected lost-repository symptom in both copies.
2. **Given** matched baseline and assisted copies, **when** agents receive the frozen task and same Codex model, **then** only the assisted copy receives BrownPants evidence and both post-fix checks run independently.
3. **Given** agent results and the hidden key, **when** the evaluator scores them, **then** fix correctness, constructor/caller coverage, precision, recall, false positives, and token usage are reported separately.
4. **Given** the paired score, **when** documentation describes it, **then** a superiority claim appears only when assisted actionable-impact recall is at least 20 percentage points higher and precision is no lower; otherwise the measured result and limitation are stated without a win claim.

### Edge Cases

- A model may return malformed JSON, skip tests, or fail to fix the bug; the scorer records the failure without inventing findings.
- Static constructor impact may identify source callers and a DI registration route without proving runtime container activation.
- One paired run may vary by model behavior; the result must identify its sample size and avoid general quality, cost, or time claims.

## Requirements

### Functional Requirements

- **FR-001**: The benchmark MUST use a new task specification with hashed acceptance criteria for the constructor-injection defect; existing frozen batch-bug inputs MUST remain unchanged.
- **FR-002**: The preparation workflow MUST create byte-identical seeded source copies, verify that the clean check passes and seeded checks fail, and keep the answer key and scorer outside both agent worktrees.
- **FR-003**: Baseline and assisted Codex runs MUST use the same model, prompt, acceptance criteria, sandbox level, repository contents, and post-fix check; only the assisted run receives BrownPants reports.
- **FR-004**: The BrownPants report MUST include compiler-resolved generic constructor construction sites, direct/transitive callers, and relevant test-path candidates with explicit evidence limits.
- **FR-005**: The scorer MUST verify output structure and match source-located findings to the hidden expected facts, reporting true positives, false positives, false negatives, precision, and recall by finding domain.
- **FR-006**: The results MUST capture Codex CLI/model identity, source and criteria hashes, pre/post check outcomes, both findings scores, and input/output token usage when the CLI exposes it.
- **FR-007**: Published documentation MUST distinguish code-fix success from impact-finding quality, state the run count, retain negative/inconclusive results, and apply the predeclared advantage rule without changing criteria after seeing scores.
- **FR-008**: The benchmark MUST preserve the existing evidence-only MFC, C++/CLI, legacy .NET Framework, and NHibernate files and MUST NOT claim they cause this managed constructor defect.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The clean fixture check passes and both matched seeded checks fail with the same expected symptom and identical seeded source hash.
- **SC-002**: Both Codex conditions receive identical frozen task inputs and run the same independent post-fix check; the captured record identifies any missing or failed run.
- **SC-003**: Every result is scored against a hidden key with per-domain TP/FP/FN, precision, recall, and observable token counts; malformed results score as failures rather than being silently excluded.
- **SC-004**: The public comparison makes a BrownPants advantage claim if and only if assisted actionable-impact recall improves by at least 20 percentage points without a precision decrease; all other outcomes are explicitly non-winning or inconclusive.

## Assumptions

- The existing .NET 10 demo check is the independent oracle; the legacy framework/native files remain inspection context and are not built.
- Codex CLI is available and authenticated in the benchmark environment. If a run cannot complete, the harness records that state and does not substitute a simulated result.
- One paired run is an initial demonstration, not a general benchmark; repeated pairs are needed before making broader performance claims.
