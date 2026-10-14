# Tasks: Versioned JSON Report

**Input**: Design documents from `specs/006-versioned-json-report/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/report-version.md

**Tests**: Required by the acceptance criteria and repository constitution. Add regression coverage before implementation.

## Phase 1: User Story 1 - Identify the report contract (Priority: P1)

**Independent test**: Successful JSON reports in default, empty, semantic, and SDD-configured scenarios expose the same version; Markdown omits the JSON marker.

- [X] T001 [US1] Add schema version assertions for normal, empty, semantic, and SDD-configured JSON reports and verify Markdown exclusion in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T002 [US1] Add a constant, serialization-ordered `SchemaVersion` property to `RepositoryInventory` in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [X] T003 [US1] Document version 1 and additive versus breaking evolution in `specs/002-cross-ecosystem-inventory/contracts/project-inventory.md` and `specs/006-versioned-json-report/contracts/report-version.md`.
- [X] T004 [US1] Explain the report version and consumer behavior in `README.md`.
- [X] T005 [US1] Validate the quickstart scenarios and full xUnit suite against `specs/006-versioned-json-report/quickstart.md`.

## Dependencies & Execution Order

- T001 precedes the model change T002.
- T003 and T004 follow the stable contract decision and can proceed independently of source implementation.
- T005 follows all changes and verifies the user story end to end.

## Implementation Strategy

Add tests first, then serialize a fixed root version without allowing callers or repository state to change it. Preserve every existing report field and keep Markdown unchanged.

