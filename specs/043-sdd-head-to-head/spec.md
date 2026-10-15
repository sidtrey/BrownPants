# Feature Specification: Vanilla SDD and BrownPants Comparison

**Feature Branch**: `043-sdd-head-to-head`

**Created**: 2026-10-15

**Status**: Draft

**Input**: FIFO ideas 7 and 12: compare baseline SDD with BrownPants on this real project, introduce the same controlled bug, measure model-reported token counts, and show a demo with a plain-English summary.

## User Scenarios & Testing

### User Story 1 - Compare the same bug task fairly (Priority: P1)

A team wants evidence whether BrownPants changes how effectively an agent detects and fixes a bug in a real codebase.

**Independent Test**: Run the same controlled bug and task against clean isolated worktrees at the same commit using the same Codex CLI model. The baseline run does not use BrownPants. The assisted run receives BrownPants repository/acceptance evidence.

**Acceptance Scenarios**:

1. **AC-001** Given identical starting commit, bug fixture, task, model, and validation commands, when baseline and assisted runs execute, then the only planned treatment difference is BrownPants evidence.
2. **AC-002** Given the controlled issue, when each run completes, then findings, code changes, regression tests, command/rework counts, and validation results are recorded against the same acceptance criteria.
3. **AC-003** Given Codex CLI JSONL output, when usage is reported, then input/output token totals come only from provider-reported `turn.completed` usage fields; missing usage is marked unavailable and never replaced by estimates.
4. **AC-004** Given the result is added to user-facing documentation, when readers review it, then host, model, commit, task, method limits, and one-run sample size are visible and conclusions are limited to this comparison.

### User Story 2 - Understand the result without SDD jargon (Priority: P1)

A non-developer wants a short plain-English explanation and demo summary in the README.

**Acceptance Scenarios**:

1. **AC-005** Given the measured comparison, when it appears in the README, then it shows paired outcomes and provider-reported token counts, and explains hashed acceptance criteria in plain English.
2. **AC-006** Given a one-run comparison, when conclusions are stated, then the README does not claim general model superiority or guaranteed savings.

## Edge Cases

- Model usage event fields may be absent; this invalidates token comparison rather than allowing estimates to masquerade as actual counts.
- A run may fail, make unrelated changes, or fail tests; record the result without rerunning only the weaker condition.
- Time and token counts can vary between agent runs; one paired run is illustrative, not statistically representative.
- Raw JSONL may include source or prompt data and must remain temporary and uncommitted.

## Requirements

### Functional Requirements

- **FR-001**: Both conditions MUST start from the same commit and controlled bug state and receive the same task and acceptance criteria.
- **FR-002**: The comparison MUST document its host, resolved model, CLI version, task, evidence treatment, tests, outcomes, and limitations.
- **FR-003**: Input/output token counts MUST be extracted from each run's observed Codex CLI usage events.
- **FR-004**: The task MUST exercise BrownPants on BrownPants and include a regression test for the controlled defect.
- **FR-005**: The README MUST present a concise scenario-based result and plain-English explanation of hashed acceptance criteria.
- **FR-006**: Raw agent traces and temporary benchmark worktrees MUST not be committed.

### Key Entities

- **Comparison run**: One isolated baseline or BrownPants-assisted attempt with a shared task and validation procedure.
- **Usage record**: Provider-reported per-run input and output token totals, model, host, and CLI version.
- **Controlled defect**: `RepositoryScanner.IsTestPath` classifies ordinary names such as `ContestController.cs` as tests because they contain the substring `test`.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Both conditions begin at the same recorded commit with the same reproduced defect and identical acceptance criteria.
- **SC-002**: Each condition has an observed outcome and full Release test result; no missing result is described as success.
- **SC-003**: Both token totals are verified from raw usage events or explicitly marked unavailable.
- **SC-004**: README comparison identifies the one-run limitation and includes the requested plain-English hash explanation.

## Assumptions

- Codex CLI is the common agent host for this first comparison; the result does not generalize to Copilot or Claude GUI sessions.
- The deterministic `RepositoryScanner.IsTestPath` false-positive is the controlled defect and is tested using `src/ContestController.cs`.
