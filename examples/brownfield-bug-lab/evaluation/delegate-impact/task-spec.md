# Frozen Implementation Task: Restore Callback Persistence

**Status**: Frozen benchmark input. Do not edit this task or its schema during a run.

**Task**: Repair the seeded single-item persistence regression through the existing delegate callback flow.

## User Scenarios & Testing

### User Story 1 - Persist through the callback (Priority: P1)

A caller imports one work item, but the repository remains empty. Repair the defect while preserving the repository and workflow APIs.

**Independent Test**: Run `dotnet run --project DelegateImpact.Fixture.csproj -c Release` from the task source root.

**Acceptance Scenarios**:

1. **AC-001** Given one work item is passed to `ImportWorkflow.ImportOne`, when the workflow returns, then exactly that item appears once in the supplied repository.
2. **AC-002** Given the repair is complete, when the fixture check runs, then it succeeds without changing the public repository or workflow APIs.
3. **AC-003** Given `WorkRepository.Save(WorkItem)` is changed, when managed impact is reported, then the answer identifies the delegate-bound target and all in-repository callers in the impact chain with correct relationship kinds and source paths.
4. **AC-004** Unsupported or runtime-only relationships are not reported as resolved.

The optional `BrownPantsEvidence/impact.json` file is available only in some task copies. Verify supplied evidence against source. Do not change benchmark controls or evidence files. Limit source changes to the smallest repair required.

## Success Criteria

- **SC-001**: The fixture check reports one saved item with ID 7 after a single-item import.
- **SC-002**: The fix preserves the repository and workflow APIs; both baseline and assisted post-fix checks succeed.
- **SC-003**: Submitted managed impact facts are source-supported, actionable, and use accurate paths, signatures, and relationship kinds.
- **SC-004**: The benchmark records frozen input hashes, all agent runs, usage, and independent post-fix check results. It does not claim superiority unless the approved pooled threshold is met.
