# Frozen Implementation Task: Restore Complete Generic Batch Saves

**Status**: Frozen experiment input
**Task**: Repair the seeded batch-save regression in the managed order desk fixture.

## User Scenarios & Testing

### User Story 1 - Persist every imported row (Priority: P1)

A developer receives a report that a mixed batch persists fewer rows than it receives. The single-item and mapped generic workflows must continue to work.

**Acceptance Scenarios**:

1. **AC-001** Given a batch with two orders, when `OrderImportWorkflow<T>.ImportBatch` persists the batch through `OrderRepository<T>`, then both orders are saved exactly once.
2. **AC-002** Given a single order or mapped source rows, when the existing workflow methods run, then their behavior remains correct.
3. **AC-003** Given calls with the same method name, when the impact findings are reported, then the batch and single-item overloads are distinguished by target signature; the unrelated `AuditJournal.Save` is not reported as a call to `OrderRepository<T>`.

## Success Criteria

- **SC-001**: The seeded check fails before implementation and all managed checks pass after the fix.
- **SC-002**: The implementation change remains limited to the managed repository behavior; no MFC or NHibernate evidence-only files are changed.
