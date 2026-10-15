# Feature Specification: Scope-Aware WinForms Control Evidence

**Feature Branch**: `053-scope-aware-winforms-evidence`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Improve brownfield WinForms analysis after reviewing feature 040: current per-file name maps can miss partial controls and can confuse same-named variables in separate scopes.

## User Scenarios & Testing

### User Story 1 - Find control writes reliably across legacy source files (Priority: P1)

A maintainer reviews a legacy C# or Visual Basic WinForms repository and needs property writes on controls located even when declarations and assignments are separated across files, aliases, partial types, and nested scopes.

**Why this priority**: Inaccurate Control recognition either hides relevant UI writes or creates noisy false candidates, reducing trust in brownfield analysis.

**Independent Test**: Analyze small multi-file C# and VB repositories containing partial and derived controls, aliases, fields/properties, nested scopes with shadowed local names, and ordinary non-Control decoys. Compare the report to a frozen expected set of source locations and evidence labels.

**Acceptance Scenarios**:

1. **AC-001** Given a partial or derived WinForms control split across repository source files, when analysis runs, then writes through its declared control members are located with the correct property and source path.
2. **AC-002** Given same-named locals in nested or separate scopes with different declared types, when analysis runs, then only writes whose receiver is a recognized Control are reported; a non-Control shadow must not inherit another scope's Control type.
3. **AC-003** Given C# aliases, qualified WinForms names, and typed fields/properties, when analysis runs, then the report recognizes Control receivers without relying only on simple identifier spelling.
4. **AC-004** Given equivalent Visual Basic cross-file, scoped-name, and ordinary-object cases, when analysis runs, then it applies equivalent candidate rules and reports correct locations.
5. **AC-005** Given malformed files, ambiguous or unresolved type evidence, and conditional/source constructs, when analysis runs, then it reports only positively recognized Control assignments and surfaces bounded coverage issues rather than guessing.
6. **AC-006** Given any repository and output limit, when analysis runs, then default analysis remains source-only, deterministic, bounded, read-only, and labels findings as candidates without claiming thread safety or runtime defects.

## Requirements

### Functional Requirements

- **FR-001**: The analyzer MUST build a repository-wide syntax index for C# and Visual Basic control declarations and declared member types before classifying assignments.
- **FR-002**: Receiver classification MUST respect declaration scope and source type evidence; same-named declarations in distinct scopes MUST NOT overwrite or contaminate each other.
- **FR-003**: The analyzer MUST recognize supported partial/derived Control types, aliases, qualified WinForms names, and typed fields/properties across source files.
- **FR-004**: Unresolved or ambiguous receivers MUST not be reported as positively recognized Control writes; limitations MUST be visible through bounded issues or coverage metadata.
- **FR-005**: Existing background callback and marshaling observations MUST remain separate from receiver-type evidence and remain candidate-only.
- **FR-006**: Existing CLI syntax and Markdown/JSON output compatibility MUST be preserved except for additive coverage information required to explain indexed files or unresolved evidence.
- **FR-007**: Default analysis MUST NOT evaluate MSBuild projects, build, execute repository code, access the network, or modify the inspected repository.
- **FR-008**: Add no runtime package dependencies.

## Success Criteria

- **SC-001**: C# fixtures identify every expected control write across files and exclude every same-name ordinary-object decoy in the answer key.
- **SC-002**: Visual Basic fixtures identify every expected control write and exclude ordinary-object decoys using equivalent scope rules.
- **SC-003**: Repeated JSON/Markdown runs produce stable finding identities and counts; emitted plus omitted equals total candidates.
- **SC-004**: The packed CLI analyzes legacy .NET Framework source without invoking project evaluation or changing files.
- **SC-005**: BrownPants self-analysis and the Brownfield Bug Lab complete with explicit source/type coverage and no unqualified safety/defect conclusions.

## Assumptions

- V1 remains offline syntax analysis. It does not load compilations or establish runtime types, partial-type linkage across incompatible namespaces/conditional configurations, caller thread identity, or test coverage.
- Only assignment targets whose receiver type can be linked from declarations and recognized WinForms inheritance are candidates; dynamic/reflection-based control access remains outside the positive set.
- The report may add bounded issue records, but it will not include source excerpts.
