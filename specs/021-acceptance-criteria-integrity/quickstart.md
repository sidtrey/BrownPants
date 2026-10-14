# Quickstart: Acceptance Criteria Integrity

## Map Tasks to Criteria

Give each new acceptance scenario an `AC-###` ID and keep existing `SC-###` IDs. Map every criterion and every task in `acceptance-evidence.json`:

```json
{
  "schemaVersion": 1,
  "tasks": [
    {
      "taskId": "T001",
      "criterionIds": ["AC-001", "SC-001"],
      "verificationCommand": "pwsh -NoProfile -File tests/AcceptanceEvidenceTests.ps1",
      "evidencePaths": ["tests/AcceptanceEvidenceTests.ps1"],
      "result": "planned"
    }
  ]
}
```

Run the validator with:

```powershell
pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 `
  -RepositoryRoot . `
  -SpecPath specs/021-acceptance-criteria-integrity/spec.md `
  -TasksPath specs/021-acceptance-criteria-integrity/tasks.md `
  -EvidencePath specs/021-acceptance-criteria-integrity/acceptance-evidence.json
```

When a task is complete, set its ledger result to `passed`, retain the command that was actually run, and cite at least one existing test or evidence file. The validator checks IDs, task states, paths, and coverage; it does not run the command or judge test semantics.

## Change an Existing Baseline

Do not change the spec and digest together without review. Add or update `acceptance-criteria-review.md` in the feature directory with the previous digest, proposed digest, reason, and changed criterion IDs. For a late first baseline on a feature that already exists on the base branch, use `none` as the previous digest. Ask a maintainer to inspect the spec diff and record, then apply the `acceptance-criteria-reviewed` pull request label. CI compares the base and head revisions and reruns when the label changes. A new feature's first baseline is its initial commitment and needs no amendment label; deleting or renaming a recorded baseline is rejected.

First verify the normal hash:

```powershell
pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 `
  -SpecPath specs/021-acceptance-criteria-integrity/spec.md
```

The review label is an approval signal, not proof of test quality or cryptographic reviewer identity. Configure the CI job as a required pull request check for merge blocking.
