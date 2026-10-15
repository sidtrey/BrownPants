# Feature Specification: Transitive Managed-to-Native Impact

**Feature Branch**: `064-transitive-managed-native-impact`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Extend BrownPants brownfield change impact so a changed managed method's callers can lead to source-located native, C++/CLI, and MFC candidate evidence.

## User Scenarios & Testing

### User Story 1 - Follow a managed change across native boundaries (Priority: P1)

A maintainer changes a managed method and wants to see native bridge and MFC command candidates that can reach any managed caller of that method, with enough depth and location information to review the chain.

**Why this priority**: A business-logic change can affect an MFC client even when the changed file itself is not the interop target. Missing that chain leaves QA and reviewers unaware of a real brownfield boundary.

**Independent Test**: In the Brownfield Bug Lab, change `OrderRepository.cs`; report the impacted `OrderImportWorkflow<T>.ImportBatch` caller and the native C++/CLI bridge, qualified C++ caller, and MFC dispatch candidates connected to its declaration.

**Acceptance Scenarios**:

1. **AC-001** Given a changed managed declaration with a transitive managed caller that is also a native-flow target, when impact is analyzed, then native candidates are traced from that exact caller declaration and retain candidate evidence labels and source locations.
2. **AC-002** Given an impact reached through both managed and native edges, when output is rendered, then native depth keeps its existing native-only meaning and the managed depth at which native tracing begins is also available.
3. **AC-003** Given an unrelated native method in the same source file, when the managed change is analyzed, then that unrelated method is not included solely because it shares a file.

### User Story 2 - Preserve direct and focused impact behavior (Priority: P2)

A developer uses direct managed/native impact or semantic evidence focus and expects existing findings and completeness limits to remain accurate while connected cross-boundary evidence is included.

**Why this priority**: BrownPants must add useful brownfield context without changing existing direct trace semantics or implying compiler certainty for lexical native relationships.

**Independent Test**: Compare direct changed-workflow impact and `evidence --semantic --changed` with the same fixtures; verify existing direct native chain, connected evidence selection, stable output, and explicit limits.

**Acceptance Scenarios**:

1. **AC-004** Given a directly changed managed interop target or native source, when impact runs, then current direct trace behavior and native depth values are preserved.
2. **AC-005** Given semantic focused evidence for a managed change, when connected native candidates are available, then those exact candidates are included with native lexical labels and unrelated candidates remain excluded.
3. **AC-006** Given missing, ambiguous, or sampled declaration/flow evidence, when tracing is requested, then uncertainty and completeness limits remain visible and no candidate is presented as resolved.

### Edge Cases

- Multiple methods declared on the same line or multiple native targets sharing a file.
- Several managed impact paths reach one declaration at different depths.
- A native relation targets a candidate location rather than one unique target.
- Declarations or native relations are sampled, absent, ambiguous, or cyclic.
- Platform-specific path casing and separators differ.

## Requirements

### Functional Requirements

- **FR-001**: Impact analysis MUST connect managed caller impacts to exact source declarations by symbol, path, and line.
- **FR-002**: Native trace MUST seed from impacted declaration locations as well as direct changed paths, without broadening selection to unrelated declarations in the same file.
- **FR-003**: Native impact MUST retain its existing native-edge depth and expose the managed impact depth associated with each managed-seeded native trace.
- **FR-004**: Markdown and JSON MUST expose deterministic, source-located candidates and clearly distinguish managed compiler-resolved relationships from native lexical candidates.
- **FR-005**: Focused semantic evidence MUST include connected native findings selected by the impact trace and MUST not include unrelated native findings solely by path.
- **FR-006**: Existing direct native and managed impact results, identifiers, and completeness behavior MUST remain compatible unless new connected findings add a relevant limitation.
- **FR-007**: Missing, ambiguous, sampled, and unresolved evidence MUST remain explicit; the analyzer MUST NOT claim runtime impact or compiler binding for native edges.
- **FR-008**: Default analysis MUST remain deterministic, bounded, read-only, offline, and independent of project evaluation or new runtime dependencies.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Changing `managed/Persistence/OrderRepository.cs` in the Brownfield Bug Lab produces native candidates for the bridge, qualified native call, and MFC dispatch reachable through the impacted `ImportBatch` declaration.
- **SC-002**: Direct impact from `managed/Workflows/OrderImportWorkflow.cs` retains its existing native candidate chain and native depths.
- **SC-003**: An unrelated method in the same managed or native file is not selected unless its exact declaration or relation is reached.
- **SC-004**: Repeated JSON and Markdown runs produce stable ordering and identities, and focused semantic evidence includes all connected candidate findings within its bounds.
- **SC-005**: Fixture tests verify ambiguity, sampling, and missing-evidence limits without turning lexical candidates into resolved calls.

## Assumptions

- Managed call impacts and semantic declarations use compatible fully qualified method identities.
- A declaration's path and line are sufficient to bind native candidate target locations to that managed impact; line-only or file-only matching is not sufficient when exact target evidence exists.
- Existing native `Depth` remains native reverse-trace distance; managed caller distance is a separate value.
- Native candidate evidence supports review and QA navigation but does not establish runtime reachability.
