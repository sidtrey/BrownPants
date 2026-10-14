# Tasks: Test BrownPants Spec Kit Extension Installation

**Input**: Design documents from `specs/008-test-brownpants-spec/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/integration-test.md`, `quickstart.md`

**Tests**: Required. Exercise the actual Spec Kit CLI installer in a temporary project, then run it on each CI host.

## Phase 1: User Story 1 - Trust the Spec Kit extension installation (Priority: P1)

**Independent test**: Install the extension with `specify` in a temporary project and verify enabled extension, declared command, all ten mandatory hook registrations, and safe cleanup.

- [X] T001 [US1] Extend `tests/SpeckitExtensionTests.ps1` to detect `specify`, install `integrations/speckit` with `--dev` in an isolated temporary `.specify` project, and clearly skip the live test only when the CLI is absent.
- [X] T002 [US1] Assert installed extension listing, manifest-declared command file, and all ten enabled mandatory hook registrations in `tests/SpeckitExtensionTests.ps1`.
- [X] T003 [US1] Restore the original directory and safely remove only the unique temporary project in `tests/SpeckitExtensionTests.ps1`, including when installation or assertions fail.
- [X] T004 [US1] Add a CI step in `.github/workflows/ci.yml` to install `specify-cli` 1.2.0 before adapter checks on Windows, Linux, and macOS.

## Phase 2: Polish and Cross-Cutting Concerns

- [X] T005 [P] Document the live installer test and its local CLI requirement in `integrations/speckit/README.md`.
- [ ] T006 Run the integration script with Spec Kit 1.2.0 locally, run Release build/tests and both adapters, push changes, and verify all three CI jobs run the non-skipped live installer check successfully.

## Dependencies & Execution Order

- T001 establishes temporary installation; T002 adds its assertions; T003 makes cleanup reliable.
- T004 ensures the required CLI is present in CI before the Spec Kit test script runs.
- T005 documents the final behavior.
- T006 depends on all implementation changes and requires green host-specific CI results.

## Implementation Strategy

Run the real installer against a unique temporary project. Preserve the existing static synchronization checks, make the live check optional only for local environments without `specify`, and make CI install a pinned CLI so the cross-platform test cannot skip there.
