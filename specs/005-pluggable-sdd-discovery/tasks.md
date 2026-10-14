# Tasks: Pluggable SDD Discovery

**Input**: Design documents from `specs/005-pluggable-sdd-discovery/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/custom-workflow-config.md

**Tests**: Required by the feature acceptance criteria and active development goal. Add regression tests before implementation.

## Phase 1: User Story 1 - Register another SDD engine (Priority: P1)

**Independent test**: A temporary repository with one valid custom declaration reports correct JSON and Markdown inventory alongside built-in workflows.

- [X] T001 [US1] Add valid custom registration, bounded sample, overlapping roots, declaration self-exclusion, and nullable change count tests in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T002 [US1] Parse valid workflow definitions and derive deduplicated custom workflow counts and paths from the scanned file list in `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs`.
- [X] T003 [US1] Pass repository root to SDD analysis and expose workflow configuration issues in `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` and `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [X] T004 [US1] Render custom workflow rows and structured diagnostics in `src/BrownPants.Cli/Output/MarkdownReport.cs`.
- [X] T005 [US1] Document the custom workflow declaration and example in `README.md`.

## Phase 2: User Story 2 - Diagnose invalid declarations safely (Priority: P1)

**Independent test**: Invalid fixtures produce diagnostics, preserve built-in inventory, and never report paths outside the fixture.

- [X] T006 [US2] Add malformed JSON, unsupported schema, unknown property, duplicate name, invalid root, too many workflows, and oversized declaration tests in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T007 [US2] Validate schema version, strict known properties, required fields, unique names, portable repository-relative roots, archive names, and configuration/workflow-count limits in `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs`.
- [X] T008 [US2] Verify mixed valid and invalid definitions preserve built-ins and produce stable diagnostics across repeated scans in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T009 [US2] Confirm generated JSON/Markdown diagnostics and security limits against `specs/005-pluggable-sdd-discovery/quickstart.md`.

## Dependencies & Execution Order

- T001 precedes T002 through T004; T003 is the model/scanner integration needed for custom analysis and diagnostics.
- T005 follows the stable contract and output behavior.
- T006 and T008 precede T007 for test-first validation.
- T009 follows all implementation tasks.
- User Story 2 depends on US1's shared configuration reader and report wiring, but its invalid-entry behavior is independently validated.

## Implementation Strategy

Deliver valid custom workflow inventory first, then invalid configuration diagnostics and hostile-root protections. Keep built-in detector outputs covered throughout. Run the .NET suite and both adapter scripts after implementation.
