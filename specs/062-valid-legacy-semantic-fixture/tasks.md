# Tasks: Valid Legacy Semantic Fixture

**Input**: Design documents in `specs/062-valid-legacy-semantic-fixture/`

## Phase 1: User Story 1 - Trust the legacy-project evidence (P1)

**Independent test**: Run semantic inspection on the Framework fixture and confirm the missing-`Compile`-target issue is gone; review remaining diagnostics without treating unresolved calls as resolved.

- [X] T001 [US1] Configure `examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj` with C# 10, Windows RID, C# targets, and an exact NHibernate 5.5.2 PackageReference/lock plus explicit restored assembly reference; replace `packages.config` and make the virtual entity non-sealed in `examples/brownfield-bug-lab/legacy/Framework/OrderDataAccess.cs`.
- [X] T002 [US1] Run opt-in semantic inspection on the legacy fixture, capture project coverage, workspace issues, compiler errors, and call-resolution results in `specs/062-valid-legacy-semantic-fixture/verification.md`.
- [X] T003 [US1] Run the managed Bug Lab check, Release build, full xUnit suite, acceptance hash, and evidence ledger; document actual prerequisites and results in `specs/062-valid-legacy-semantic-fixture/verification.md`.
- [X] T004 [US1] Update `examples/brownfield-bug-lab/README.md` with the legacy project's restore, build, semantic-inspection commands, and platform/evaluation prerequisites.

## Dependencies and Order

- T002 depends on T001 because it verifies the corrected project metadata.
- T003 follows T002 so verification records the final semantic evidence.
