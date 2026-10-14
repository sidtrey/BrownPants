# Tasks: Cross-Platform Validation

**Input**: Design documents from `specs/007-validate-brownpants-builds/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/ci-validation.md`, `quickstart.md`

**Tests**: Required by the feature acceptance criteria. Validate locally on Windows and use the pushed GitHub Actions matrix for Linux/macOS runtime evidence.

## Phase 1: User Story 1 - Trust BrownPants on the host platform (Priority: P1)

**Independent test**: Windows, Linux, and macOS jobs each pass restore, Release build, all xUnit tests, both adapter checks, packaging, installed CLI invocation, and JSON schema/file-count assertions.

- [X] T001 [US1] Change `.github/workflows/ci.yml` to run the existing validation job on `windows-latest`, `ubuntu-latest`, and `macos-latest`.
- [X] T002 [US1] Make the packed-tool smoke step in `.github/workflows/ci.yml` select `brownpants.exe` on Windows and `brownpants` on Linux/macOS; assert JSON parses, `SchemaVersion` is 1, and `FileCount` is nonzero.
- [X] T003 [US1] Confirm the existing Windows-specific project fixture in `tests/BrownPants.Tests/RepositoryScannerTests.cs` verifies MFC/CLR metadata and references without evaluating or building the fixture.
- [X] T004 [US1] Update `README.md` with the supported CI host matrix and explain that Windows-specific projects are inventoried cross-platform but are not built by the scanner.

## Phase 2: User Story 2 - Diagnose host-specific failures (Priority: P2)

**Independent test**: Introduce or inspect a failed host result and confirm other OS jobs complete with separately visible results.

- [X] T005 [US2] Configure matrix failure behavior and host display names in `.github/workflows/ci.yml` so one host failure does not cancel or obscure other host outcomes.

## Phase 3: Polish and Cross-Cutting Concerns

- [X] T006 [P] Update `specs/007-validate-brownpants-builds/quickstart.md` and `contracts/ci-validation.md` to match the final workflow commands and platform-specific tool names.
- [X] T007 Run `dotnet restore`, Release build/tests, both adapter scripts, and packed-tool smoke on the local Windows host; push the change and verify all three GitHub Actions matrix jobs succeed.

## Dependencies & Execution Order

- T001 and T002 update the same workflow and should be implemented together in order.
- T003 is a coverage review; if the existing fixture does not prove the requirement, add assertions before completing T003.
- T005 overlaps T001's matrix but remains a separately reviewable acceptance requirement and can be completed in the same edit.
- T004 and T006 follow the final workflow shape.
- T007 depends on all implementation and documentation changes.

## Implementation Strategy

Deliver the three-host build and installed-tool check first. Preserve the existing Windows MFC use case and collect separate results for all hosts. The feature is complete only after the pushed workflow has successful Windows, Linux, and macOS results.
