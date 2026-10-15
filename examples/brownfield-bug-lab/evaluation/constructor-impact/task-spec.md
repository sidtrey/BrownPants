# Frozen Implementation Task: Preserve the Injected Repository

**Status**: Frozen benchmark input
**Task**: Repair the order workflow constructor regression in the managed order-desk fixture.

## User Scenarios & Testing

### User Story 1 - Persist through the supplied repository (Priority: P1)

A service is constructed with a repository, but later writes are missing from that repository. Restore the intended dependency-injection behavior without changing the import workflow contract.

**Independent Test**: Run the managed checks before and after the repair.

**Acceptance Scenarios**:

1. **AC-001** Given an `OrderImportWorkflow<T>` constructed with an `OrderRepository<T>`, when single-item, batch, or mapped imports run, then each operation writes to that same supplied repository exactly once.
2. **AC-002** Given the factory and DI registration create `OrderImportWorkflow<Order>`, when the registered workflow resolves, then it retains the repository supplied through the factory.
3. **AC-003** Given the repair is complete, when repository changes are reviewed, then only the managed behavior needed for constructor injection changes; evidence-only legacy, native, and ORM files remain unchanged.

## Requirements

- **FR-001**: Preserve the repository instance supplied to the workflow constructor.
- **FR-002**: Keep single-item, batch, and generic mapped import behavior passing.
- **FR-003**: Identify source-located explicit constructor call sites and relevant test-path evidence; distinguish method relationships from constructor relationships.
- **FR-004**: Do not claim that source evidence proves runtime DI activation or native/ORM behavior.

## Success Criteria

- **SC-001**: The clean check passes and the seeded constructor defect fails with the expected single-item repository assertion.
- **SC-002**: After repair, the same check prints `LegacyOrderDesk checks passed.` and the single-item, batch, and mapped assertions all pass.
- **SC-003**: The repair leaves the evidence-only legacy/native/ORM files unchanged.

## Assumptions

- The managed .NET 10 fixture is the executable oracle; legacy .NET Framework, MFC/C++/CLI, and NHibernate artifacts are context only.
- The evaluation report describes source-level callers, not proof of runtime execution.
