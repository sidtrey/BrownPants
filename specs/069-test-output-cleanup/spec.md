# Feature Specification: Generated Test Output Cleanup

**Feature Branch**: `069-test-output-cleanup`

**Created**: 2026-10-15

**Status**: Draft

**Input**: User request: Automatically clean old generated test and QA data because repeated validation runs are accumulating confusing scratch output.

## User Scenarios & Testing

### User Story 1 - Keep successful QA runs tidy (Priority: P1)

As a contributor, I want successful test and QA runs to remove old generated output automatically while preserving recent evidence and failures.

**Independent Test**: Create synthetic successful and failed run folders with controlled ages, run cleanup, and verify only eligible BrownPants-owned paths are removed.

**Acceptance Scenarios**:

1. **AC-001** Given isolated generated run folders under the ignored BrownPants test-output root, when a test or QA run succeeds, then cleanup removes only runs older than the documented age or beyond the documented successful-run retention count and reports each removal.
2. **AC-002** Given a failed run, when automatic cleanup runs, then its output remains available for at least seven days; dry-run reports eligible paths without deleting them.
3. **AC-003** Given a path outside the managed root or a symbolic link/reparse point, when cleanup is requested, then it refuses the unsafe target and never traverses or deletes outside the managed root.
4. **AC-004** Given legacy leftovers, when a contributor invokes the cleanup review command, then it reports managed and unmanaged entries; explicit removal remains limited to paths carrying BrownPants ownership markers.

### Edge Cases

- Missing roots and empty roots are successful no-ops.
- Malformed manifests, timestamps, and reparse points are preserved and reported as skipped.
- Cleanup never runs after a failed test/QA process.

## Requirements

### Functional Requirements

- **FR-001**: Test and QA automation MUST place generated run artifacts in unique subdirectories beneath an ignored, BrownPants-managed root.
- **FR-002**: Successful runs MUST invoke bounded retention cleanup; failed runs MUST retain artifacts for at least seven days.
- **FR-003**: Cleanup MUST validate canonical paths and ownership markers, reject links/reparse points, and report removed/skipped paths.
- **FR-004**: Contributors MUST have one dry-run review and explicit cleanup command for stale managed output.
- **FR-005**: Cleanup MUST NOT delete fixtures, source files, documentation examples, user outputs, or unmarked legacy directories.
- **FR-006**: Retention policy MUST be configurable through documented age/count options with conservative defaults.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Repeated successful QA runs leave no more than five successful managed runs and no successful run older than 14 days.
- **SC-002**: Failed run output remains for at least seven days unless explicitly removed by the contributor.
- **SC-003**: Automated safety tests prove dry-run behavior, age/count retention, failed-run preservation, link rejection, and refusal to delete outside the managed root.
- **SC-004**: A contributor can inspect and clean eligible managed leftovers with one documented command.

## Assumptions

- Generated run output belongs under `TestResults/.brownpants/runs/` and remains ignored by Git.
- “Automatic” means wrappers/scripts that own test or QA runs invoke cleanup only after a successful process exit.
- Existing unmarked `TestResults` content is never assumed to be disposable.
