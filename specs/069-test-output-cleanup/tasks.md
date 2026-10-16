# Tasks: Generated Test Output Cleanup

## Phase 1: Managed run lifecycle

- [x] T001 Add shared PowerShell helper for safe managed-root resolution, run manifests, and retention selection.
- [x] T002 Add a test wrapper that writes `dotnet test` results into a unique managed run and records success/failure.
- [x] T003 Update chart QA to write each run into a unique managed directory and prune only after success.

## Phase 2: Cleanup command and verification

- [x] T004 Add a review-by-default cleanup command with explicit `-Apply`, age, and count options; show unmanaged legacy entries without deleting them.
- [x] T005 Add PowerShell tests for dry-run, successful age/count pruning, failed-run preservation, malformed markers, root escapes, and reparse points.
- [x] T006 Document the wrapper and cleanup command; keep generated run roots ignored.
- [x] T007 Run script tests, Release build and test suite, BrownPants self-analysis/QA, acceptance hash, and evidence ledger.
