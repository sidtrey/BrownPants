# Feature Specification: Show BrownPants Brownfield Value

**Feature Branch**: `047-brownfield-value-demo`
**Created**: 2026-10-15
**Status**: Completed
**Input**: Redesign the README head-to-head so readers quickly see the practical value BrownPants adds to an SDD task on a real codebase, with concrete source relationships and honest comparison limits.

## User Scenarios & Testing

### User Story 1 - See what BrownPants adds to an SDD task (Priority: P1)

A developer evaluating BrownPants wants to understand what Spec Kit already provides, what BrownPants adds from the existing codebase, and why that extra context changes implementation and QA decisions.

**Independent Test**: Review the README and detailed comparison against BrownPants output, source locations, completed-run records, and validation results.

**Acceptance Scenarios**:

1. **AC-001** Given the same BrownPants bug-fix task, when the README compares Spec Kit with Spec Kit + BrownPants, then it explains that both workflows produced the same fix and focuses the difference on BrownPants' concrete brownfield evidence and its practical use for risk assessment and QA.
2. **AC-002** Given the impact report for `RepositoryScanner.cs`, when the detailed report explains BrownPants value, then it names source-verified consumers of test-path classification and connects the behavior to repository inventory, bug investigation, and call-impact reporting, with source paths and line references.
3. **AC-003** Given the experiment records, when the comparison describes outcomes, then it distinguishes the expected seeded pre-fix failure from final validation and clearly states that both formal runs completed and both passed the focused and full test suites.
4. **AC-004** Given observed BrownPants metrics, when they are published, then the report explains which figures cover the whole changed file versus a single method, labels static call evidence and test-path candidates accurately, and makes no unsupported quality, time, or token-savings claim.
5. **AC-005** Given the prior comparison report, when the replacement is published, then unsuccessful preliminary attempts are clearly identified as excluded from the formal pair and are not presented as failures of either measured result.
6. **AC-006** Given the README comparison, when a developer scans it, then it uses readable scenario steps to show what a developer and QA learn, avoids a dense comparison table, and links to the detailed evidence.

## Requirements

- **FR-001**: Use BrownPants itself as the analyzed repository and cite verifiable source paths and line numbers.
- **FR-002**: Separate observed BrownPants output, source-confirmed behavior, and interpretation.
- **FR-003**: State the measured coding outcome honestly even when both conditions produce the same fix.
- **FR-004**: Keep raw session logs, credentials, private machine details, and generated report dumps out of published files.
- **FR-005**: Explain static analysis limits and avoid presenting candidates or call counts as runtime proof, test coverage, or effort estimates.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A reader can identify in under one minute what Spec Kit supplies and what BrownPants adds.
- **SC-002**: Each stated source relationship and result maps to a cited source location or recorded experiment artifact.
- **SC-003**: A reviewer can distinguish the expected seeded failure, excluded preliminary attempts, and successful formal validations.
- **SC-004**: The comparison highlights a concrete implementation or QA decision enabled by BrownPants evidence without claiming the agent coded better in this pair.

## Assumptions

- The controlled pair remains a valid illustration of workflow inputs, but is not evidence that BrownPants improves the fix itself in every task.
- BrownPants semantic impact output is static evidence; dynamic behavior, reflection, runtime configuration, and test coverage require separate validation.
