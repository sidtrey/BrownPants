# Verification: Generated Test Output Cleanup

## Behavior and agent/human access

- Agents and people can run `scripts/test.ps1`; it isolates test results and invokes cleanup only after `dotnet test` exits successfully.
- `scripts/qa-chart-engine.ps1` gets the same unique run directory and marks failed runs for retention when its QA command errors.
- People can preview with `pwsh -NoProfile -File scripts/cleanup-test-data.ps1`; `-Apply` removes only eligible marked runs. Existing unmarked entries are reported and preserved.
- Default policy retains the five newest successful runs and removes successful runs older than 14 days. Failed runs are retained seven days. Active, malformed, unmarked, or reparse-point runs are skipped.
- The feature removes recurring clutter for developers and agents that run validation repeatedly. A reviewed migration workflow for old unmarked scratch data is the meaningful follow-up; cleanup correctly avoids guessing which legacy files are disposable.

## Evidence

- `pwsh -NoProfile -File tests/TestRunArtifactsTests.ps1` — passed. Covered dry-run, successful age/count pruning, failed-run retention, legacy preservation, and a symlink inside an eligible run; the external target remained intact.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed with 0 warnings and 0 errors.
- `pwsh -NoProfile -File scripts/test.ps1 -NoBuild` — passed, 342/342 xUnit tests; generated output was placed under `TestResults/.brownpants/runs/`.
- `pwsh -NoProfile -File scripts/qa-chart-engine.ps1` — passed; BrownPants chart callers matched independent semantic impact evidence (4 callers, 5 call sites, 10 XML edges). The run was written to its own managed folder.
- The review command reported 486 old, unmarked entries under `TestResults` and preserved them. These have no ownership marker, so automatic cleanup intentionally leaves them for individual review.
- Acceptance baseline: `2c3a130b54f2d3f0d631bc641e48f9b58df5ca385c5dc589542127d34a16782b`.

## Remaining limits

The current cleanup system manages only runs created through its wrappers. Existing unmarked `TestResults` output requires manual review and is not auto-deleted. The script tests use temporary roots and do not delete repository test output.
