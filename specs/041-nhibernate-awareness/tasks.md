# Tasks: NHibernate Awareness

**Input**: Design documents in `/specs/041-nhibernate-awareness/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, and `contracts/nhibernate-signals.md`.

**Tests**: Required for AC-001–AC-005 and SC-001–SC-004. Use temporary mixed-language repositories with offline-only fixtures.

## Phase 1: Metadata and mapping evidence

**Independent Test**: Recognize direct NHibernate package metadata and explicit mapping/config XML; reject unrelated package names and malformed/non-mapping XML.

- [x] T001 [US1] Add temporary-repository tests for C#/VB API evidence, direct PackageReference/packages.config metadata, hibernate.cfg.xml, `.hbm.xml` markers, lookalike comments/strings/types, malformed XML, ordering and sample bounds in `tests/BrownPants.Tests/NHibernateAwarenessTests.cs`.
- [x] T002 [US1] Add safe direct package and XML mapping/configuration candidate extraction to `ArchitecturePatternAnalyzer.cs`; prohibit DTDs and external resolution, preserve bounded results, and emit parse/read gaps without evaluating project files.

## Phase 2: Source API evidence and reporting

**Independent Test**: Find source-located C#/VB factory, session, query, transaction, and DI evidence only with NHibernate namespace or type evidence; repeated inspection remains stable and read-only.

- [x] T003 [US1, US2] Add guarded C#/VB syntax recognition for NHibernate namespace/type imports, session factories/sessions, queries, transactions, and DI registration; add positive and negative tests for ambiguous API names in `ArchitecturePatternAnalyzer.cs` and `NHibernateAwarenessTests.cs`.
- [x] T004 [US2] Document command examples, candidate meanings, offline behavior, and runtime/transitive-package limits in `README.md`; record focused tests, full Release build/tests, package audit, acceptance hashes, and BrownPants self-inspection in `specs/041-nhibernate-awareness/verification.md`.

## Dependencies

- T001 precedes T002 and T003.
- T004 is the completion gate and depends on T001–T003.
