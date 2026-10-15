# Feature Specification: Reproducible Brownfield SDD Comparison

**Feature Branch**: `046-brownfield-sdd-comparison`
**Created**: 2026-10-15
**Status**: Completed
**Input**: Replace the existing comparison with a real, inspectable Spec Kit versus Spec Kit + BrownPants walkthrough on the BrownPants repository. Introduce the same controlled defect in both runs and show concrete inputs, output diffs, and test evidence.

## User Scenarios & Testing

### User Story 1 - Trust the head-to-head result (Priority: P1)

A developer wants to see what Spec Kit provides on its own and what BrownPants adds when both are used on the same existing codebase and bug.

**Why this priority**: The README comparison is a central product demo; misleading labels, invented fixtures, or unexplained outcomes damage trust.

**Independent Test**: Re-run two isolated Spec Kit tasks from identical source and feature artifacts, with the same intentionally introduced defect. The only planned difference is BrownPants evidence and integration use in the assisted run.

**Acceptance Scenarios**:

1. **AC-001** Given the same pinned source revision, seeded defect, Spec Kit artifacts, prompt, model, host, and validation commands, when both runs execute, then the report identifies the one planned treatment difference and provides hashes or exact values for all shared inputs.
2. **AC-002** Given the seeded defect and new regression criterion, when each run starts, then the defect is independently reproduced as a failing check before implementation; when each run ends, then the same check and full validation are recorded with observed results.
3. **AC-003** Given two agent runs, when results are published, then each side shows its actual changed files, concise diff summary, task/acceptance evidence, commands and outcomes, review findings, and provider-reported usage or an explicit unavailable status.

### User Story 2 - Understand the added value quickly (Priority: P1)

A reader wants a direct, readable explanation of what Spec Kit gives them and what Spec Kit plus BrownPants adds, without mistaking one run for a general benchmark.

**Independent Test**: Review the README against the detailed comparison report and verify every claim maps to an observed artifact or is labeled as product capability rather than experimental result.

**Acceptance Scenarios**:

1. **AC-004** Given the verified comparison, when the README presents it, then it names the actual Spec Kit workflow, separates shared workflow from BrownPants additions, and includes concrete code/test outcomes without a dense comparison table.
2. **AC-005** Given observed token usage, when it is shown, then counts come from Codex provider usage events, the sample size and variability limits are explicit, and no causal savings or quality claim is made from one pair.
3. **AC-006** Given the final report, when it is shared, then raw session logs, credentials, and private host details are absent; the report contains enough setup, commands, diffs, and hashes for an independent reviewer to audit the conclusion.

## Edge Cases

- If either agent run fails, is interrupted, or produces invalid usage data, record the outcome and do not silently substitute a rerun.
- If model, host, source, prompt, specification, or validation differs between conditions, the pair is invalid and must not be presented as controlled.
- If the injected defect fails to reproduce before either agent runs, stop and repair the experiment setup before recording results.
- If a test passes but human review finds a defect, document it and do not call the implementation correct.

## Requirements

### Functional Requirements

- **FR-001**: The comparison MUST use the BrownPants source repository and an actual source-level defect introduced identically into both isolated worktrees.
- **FR-002**: Both conditions MUST consume the same Spec Kit specification, plan, task list, acceptance criteria, implementation prompt, model, host version, and validation procedure.
- **FR-003**: The baseline MUST use Spec Kit without BrownPants context or lifecycle checks; the assisted condition MUST document the BrownPants commands, adapter/hooks, and evidence it actually used.
- **FR-004**: The defect MUST have a failing pre-run reproduction check and an explicit regression criterion shared by both conditions.
- **FR-005**: The report MUST include the exact setup and task, source and artifact hashes, per-run changed paths and diff summaries, commands/results, review findings, provider usage provenance, and limitations.
- **FR-006**: The README MUST distinguish product capabilities from observed demo results and explain the Spec Kit versus Spec Kit + BrownPants outcome in readable scenario form.
- **FR-007**: The comparison MUST be reproducible by a maintainer with local prerequisites and user-owned model authentication; it MUST NOT publish raw model session logs or credentials.
- **FR-008**: Any claim about token counts MUST come only from the provider-reported completion usage for the corresponding observed run.

### Key Entities

- **Comparison condition**: One isolated SDD implementation run with pinned shared inputs and a declared BrownPants treatment.
- **Seeded defect**: A temporary, source-level regression in BrownPants code reproduced by the same automated check in each condition.
- **Evidence record**: Sanitized commands, hashes, diffs, test outcomes, review findings, and provider usage provenance for a condition.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Both runs start from identical recorded source, defect, spec, plan, tasks, acceptance digest, prompt, model, and host inputs.
- **SC-002**: The injected defect fails a named automated check before implementation in both worktrees, and both final worktrees receive the same full validation commands.
- **SC-003**: Every published code/test outcome can be inspected in the report or independently reproduced with its included commands.
- **SC-004**: The README comparison is readable without a table, distinguishes capability from measured outcome, and links the detailed evidence.
- **SC-005**: A reviewer can verify provider usage against captured raw logs retained only in temporary local storage and confirm no secrets or raw logs were published.

## Assumptions

- Spec Kit and Codex CLI are the first comparison workflow; results do not generalize to other hosts or models.
- A one-pair demo illustrates a concrete workflow and does not estimate general productivity, quality, or token savings.
- The intentional defect and raw run logs are temporary experimental inputs; only sanitized evidence and reproducible setup instructions are committed.
