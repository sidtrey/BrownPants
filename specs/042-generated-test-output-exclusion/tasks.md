# Tasks: Generated Test Output Exclusion

**Input**: Design documents in `/specs/042-generated-test-output-exclusion/`

**Prerequisites**: `spec.md`, `plan.md`, `checklists/requirements.md`.

**Tests**: Required by AC-001–AC-004 and SC-001–SC-004; use temporary directories so existing TestResults data remains untouched.

## Phase 1: Default discovery and explicit-root behavior

**Independent Test**: A temporary repository scan omits configured generated child folders while retaining ordinary test/source folders and includes all contents if an omitted folder itself is selected as root.

- [x] T001 [US1, US2] Add tests for case-insensitive omission of `TestResults`, `test-results`, and `testresults`, retention of `tests`/`fixtures`, explicit-root inclusion, stable ordering, and unchanged fixture files in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T002 [US1, US2] Add the three output names to `RepositoryScanner.IgnoredDirectories` and document default exclusion plus explicit-root usage in `README.md`.

## Phase 2: Release verification

- [x] T003 [US1, US2] Run focused scanner tests, Release build/full tests, acceptance hash/evidence validation, and BrownPants self-inspection; record observed outcomes in `specs/042-generated-test-output-exclusion/verification.md`.

## Dependencies

- T001 precedes T002 so the behavior is pinned before the scanner change.
- T003 is the completion gate after T001 and T002.
