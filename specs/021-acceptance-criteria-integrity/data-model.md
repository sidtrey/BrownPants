# Data Model: Acceptance Criteria Integrity

## Acceptance Criterion IDs

- `AC-###` identifies one acceptance scenario in a feature spec.
- `SC-###` identifies one measurable success criterion.
- IDs are unique within the feature and are part of the text frozen by the SHA-256 sidecar.

## Task Evidence Ledger

`acceptance-evidence.json` contains `schemaVersion: 1` and a `tasks` array. Each task appears once and contains:

- `taskId`: an ID present in `tasks.md`, such as `T004`.
- `criterionIds`: one or more valid `AC-###` or `SC-###` identifiers.
- `verificationCommand`: a concrete command to run for the task. The validator displays but never executes it.
- `evidencePaths`: repository-relative test or evidence files. Completed tasks must point to at least one existing file.
- `result`: `planned` for an incomplete task or `passed` for a completed task.

The validator checks structural correspondence and path safety. A `passed` value is a contributor's record of an observed result, not proof that the test semantically covers the criterion.

## Baseline Review Record

`acceptance-criteria-review.md` is feature-local and contains four required sections. A late first baseline on an existing feature uses `none` for the previous digest; an existing baseline uses its SHA-256 value.

```markdown
## Previous digest
<64-character SHA-256>

## Proposed digest
<64-character SHA-256>

## Reason
<why the accepted scope must change>

## Changed criteria
- AC-001: <summary>
- SC-002: <summary>
```

The checker compares both digests with the base/head sidecars and requires the record itself to change in the pull request. A maintainer adds the `acceptance-criteria-reviewed` label after reviewing the record and spec diff.
