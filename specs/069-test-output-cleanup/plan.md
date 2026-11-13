# Implementation Plan: Generated Test Output Cleanup

## Technical Context

- **Runtime**: PowerShell 7+ orchestration with .NET 10 CLI and xUnit.
- **Storage**: Ignored `TestResults/.brownpants/runs/<run-id>/` directories with a small ownership/status manifest.
- **Safety**: Cleanup is shallow at the run-root boundary, validates canonical containment and manifests, rejects reparse points, and never interprets arbitrary legacy `TestResults` folders as disposable.
- **Integration**: A test wrapper and chart QA script create per-run directories and invoke pruning only after successful commands.

## Design Decisions

- Use repository-owned PowerShell scripts and JSON manifests; add no package dependency.
- Default retention is five successful runs, 14 days for successful runs, and seven days for failed runs. Failure retention uses the longer protection period even when the count is exceeded.
- Make review the default; require `-Apply` for explicit manual pruning. Automated wrappers pass `-Apply` after success.
- Record run state transitions (`running`, `succeeded`, `failed`) and UTC completion time. Never prune `running` or malformed runs.

## Validation

- Script-level tests create isolated roots and cover review mode, age/count selection, failure retention, malformed ownership, and symlink/reparse rejection.
- Run `dotnet build BrownPants.sln -c Release`, `dotnet test BrownPants.sln -c Release`, the PowerShell acceptance scripts, and BrownPants-on-BrownPants QA.

## Constitution Check

- Evidence first: every deletion is listed; skipped evidence remains visible.
- Safe by default: only marked children of the canonical managed root can be deleted; manual review does not mutate.
- Deterministic and reviewable: stable path ordering and explicit retention options.
- Specified and verified: feature acceptance criteria and script-level fixtures gate the implementation.
