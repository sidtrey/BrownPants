# Feature Specification: Acceptance Criteria Integrity

**Feature Branch**: `021-acceptance-criteria-integrity`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Strengthen the acceptance-criteria hash gate with task-level evidence and reviewed baseline updates.

## User Scenarios & Testing

### User Story 1 - Trace tasks to acceptance evidence (Priority: P1)

During implementation, a contributor needs each planned task tied to the acceptance criteria it advances. When a task is marked complete, the record should point to observed verification evidence so completion cannot be claimed without showing how the requirement was checked.

**Why this priority**: A frozen spec prevents silent criteria changes, while task-level evidence prevents an LLM from declaring work complete without testing the original requirements.

**Independent Test**: Validate small spec, task, and evidence fixtures; complete mappings pass and missing or inconsistent mappings fail with actionable diagnostics.

**Acceptance Scenarios**:

1. **AC-001** — **Given** a feature spec with identified acceptance scenarios and success criteria plus a task list, **When** evidence is recorded, **Then** every criterion and task maps to valid IDs and a verification method.
2. **AC-002** — **Given** a task marked complete, **When** its evidence is checked, **Then** it has a passing result and at least one existing repository-relative evidence file.
3. **AC-003** — **Given** an incomplete task, **When** its evidence is checked, **Then** it is recorded as planned and is not represented as passing.
4. **AC-004** — **Given** missing criteria coverage, unknown IDs, duplicate task records, or a missing evidence file, **When** the ledger is checked, **Then** validation fails and identifies the specific mismatch.

### User Story 2 - Review intentional baseline changes (Priority: P1)

A contributor has an approved reason to change acceptance criteria. The change must be visible against the pull request base and accompanied by a review record and an explicit reviewer signal; changing the spec and its hash together must not silently pass.

**Why this priority**: The current hash verifier catches stale hashes but cannot tell whether a baseline and spec were changed together intentionally or to move the goalposts.

**Independent Test**: Compare synthetic Git histories and pull request review inputs; new feature baselines pass, existing baseline changes require the review signal and matching change record, and baseline deletion fails.

**Acceptance Scenarios**:

1. **AC-005** — **Given** a baseline already present on the pull request base, **When** its digest changes without the `acceptance-criteria-reviewed` label and a valid review record, **Then** the check fails.
2. **AC-006** — **Given** an existing baseline changes with the review label and a record containing matching old/new digests, a reason, and changed criterion IDs, **When** the pull request check runs, **Then** it passes and reports the reviewed update.
3. **AC-007** — **Given** a new feature adds its first acceptance baseline, **When** CI checks the pull request, **Then** no rebaseline approval is required.
4. **AC-008** — **Given** a previously recorded baseline is deleted, **When** CI checks the pull request, **Then** the change fails and preserves the audit history.

### User Story 3 - Apply the same integrity process in Spec Kit (Priority: P2)

An agent implementing a Spec Kit feature needs a repeatable process to freeze acceptance criteria, map tasks to criteria, record actual evidence, and request review for an intentional rebaseline.

**Why this priority**: The rules must be part of the workflow instructions that agents actually follow, not merely scripts contributors may overlook.

**Independent Test**: Inspect synchronized Spec Kit guidance and integration tests for the before-implementation hash check, task evidence process, completion check, and reviewed-change procedure.

**Acceptance Scenarios**:

1. **AC-009** — **Given** an implementation task starts, **When** the Spec Kit workflow is followed, **Then** the active baseline is verified before edits and criteria IDs are mapped to tasks.
2. **AC-010** — **Given** implementation is complete, **When** the workflow is finalized, **Then** each completed task has observed evidence, the baseline is reverified, and every acceptance criterion is accounted for.
3. **AC-011** — **Given** a contributor proposes to change frozen criteria, **When** the workflow is followed, **Then** implementation pauses until the reason and changed criteria are reviewed and the pull request carries the approval label.

## Edge Cases

- A criterion is not mapped to any task.
- A task references an unknown or duplicate criterion or task ID.
- A task is marked complete while its evidence record is planned, missing, or points outside the repository.
- A baseline file is added, modified, renamed, or deleted in a pull request.
- A review record contains stale hashes or omits the reason or changed criterion IDs.
- Acceptance wording changes while the baseline remains unchanged.
- A pull request gains or loses the review label after the first check.
- A feature has no acceptance scenarios or success criteria to map.

## Requirements

### Functional Requirements

- **FR-001**: Feature acceptance scenarios MUST have stable `AC-###` identifiers and success criteria MUST retain their `SC-###` identifiers.
- **FR-002**: Each feature task MUST be mapped to at least one valid acceptance or success criterion in a machine-readable evidence ledger.
- **FR-003**: Every criterion MUST map to at least one task; task and criterion identifiers MUST exist in their source artifacts and MUST NOT be duplicated in the ledger.
- **FR-004**: A task marked complete MUST have a passing evidence result and at least one existing repository-relative evidence path. An incomplete task MUST NOT be recorded as passing.
- **FR-005**: Evidence validation MUST be deterministic, read-only, offline, and MUST NOT execute commands supplied by the ledger.
- **FR-006**: A change to an existing baseline in a pull request MUST require the `acceptance-criteria-reviewed` label and a review record with matching old and new digests, a reason, and changed criterion IDs.
- **FR-007**: A new feature's first baseline MUST NOT require rebaseline approval; adding a baseline to a feature whose spec already exists on the base branch requires review with `none` as the previous digest. Deletion of a baseline already present on the base branch MUST fail.
- **FR-008**: The pull request review check MUST rerun when the review label is added or removed, and MUST compare the pull request head against its base commit.
- **FR-009**: Spec Kit guidance MUST require baseline verification before and after implementation, task-to-criterion mapping, observed task evidence, and explicit review before changing criteria.
- **FR-010**: Reports and failures MUST distinguish structural validation from proof that a test semantically verifies a requirement; recorded evidence remains subject to human review.

### Key Entities

- **Acceptance criterion**: A stable `AC-###` scenario or `SC-###` success criterion in a feature spec.
- **Task evidence entry**: A task ID, one or more criterion IDs, a verification command, evidence paths, and planned or passed result.
- **Baseline review record**: The old and new digests, reason, changed criterion IDs, and reviewer signal for a previously established baseline.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Evidence validation accepts complete fixtures and rejects each missing, unknown, duplicate, or inconsistent criterion/task mapping with a diagnostic identifying the affected ID.
- **SC-002**: No task marked complete passes validation without a `passed` result and an existing repository-relative evidence path.
- **SC-003**: Pull request checks reject an existing baseline change without both the approval label and a matching review record, and accept a valid reviewed change.
- **SC-004**: New feature baselines pass without review approval, while deletion of an existing baseline fails.
- **SC-005**: Label addition and removal each rerun the review check, and the checker evaluates the current pull request head against its base commit.
- **SC-006**: Spec Kit guidance and automated tests cover baseline checks before/after implementation and task evidence completion.
- **SC-007**: Validation remains read-only and offline; the evidence checker never executes commands from a ledger.

## Assumptions

- Acceptance scenarios use explicit `AC-###` IDs for new or updated feature specs; success criteria keep their existing `SC-###` IDs.
- The evidence ledger records test commands and repository-relative files, but it does not claim to prove that those tests fully establish product behavior.
- Intentional updates to an existing baseline are reviewed through a pull request. A maintainer applies `acceptance-criteria-reviewed` only after reviewing the record and diff.
- New feature baselines are initial commitments, not amendments to an existing commitment.
- Existing feature tasks are brought under task-evidence validation when their task artifacts change; historical completed features are not rewritten as part of this feature.
