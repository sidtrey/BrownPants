# Feature Specification: Native Call Impact

**Feature Branch**: `055-native-call-impact`
**Created**: 2026-10-15
**Status**: Completed
**Input**: Extend BrownPants brownfield impact tracing so QA and maintainers can follow changes in MFC and C++/CLI code through source-located native candidates.

## User Scenarios & Testing

### User Story 1 - Trace impact from changed native code (Priority: P1)

A maintainer changes a native source file in a mixed MFC/managed repository and wants to identify upstream commands, handlers, and native bridge methods that may be affected, along with confidence and source locations.

**Why this priority**: Native client and interop changes are currently excluded from change impact even though BrownPants can identify MFC dispatch and qualified native calls.

**Independent Test**: Run an impact analysis on the Brownfield Bug Lab's MFC view and bridge paths; compare the reported caller chain, paths, locations, and evidence labels with the fixture source.

**Acceptance Scenarios**:

1. **AC-001** **Given** a changed C++ file containing a uniquely mapped MFC handler, **When** impact is requested, **Then** the report includes its upstream message-map entry and handler path with candidate status and locations.
2. **AC-002** **Given** a changed native target called by a mapped handler, **When** impact is requested, **Then** the report traces the qualified native call back to the handler and MFC command map.
3. **AC-003** **Given** an unresolved or ambiguous native relation, **When** impact is requested, **Then** it remains visible as incomplete candidate evidence and is not selected as a confirmed call.

### User Story 2 - Combine managed and native evidence (Priority: P2)

A maintainer changes a file in a mixed repository and needs managed compiler-resolved impact and native lexical candidates presented together while retaining distinct evidence types and limits.

**Why this priority**: QA needs a single change review that covers mixed-language boundaries without treating different evidence strengths as equivalent.

**Independent Test**: Select changed C#, C++ and C++/CLI files in one fixture and verify separate managed and native impact records, stable ordering, and completeness explanations.

**Acceptance Scenarios**:

1. **AC-004** **Given** multiple changed managed and native files, **When** impact runs, **Then** each file is evaluated by its supported evidence source and findings identify that evidence source.
2. **AC-005** **Given** both candidate and resolved relationships, **When** Markdown or JSON is requested, **Then** the report distinguishes them and preserves existing managed impact behavior.

### Edge Cases

- Changed native files have no matching mapped or qualified relations.
- Native relations are missing, ambiguous, sampled, or point outside the discovered source set.
- Changed paths are absolute, traverse parent directories, are missing, or use unsupported file extensions.
- Repeated input paths and cycles must not duplicate findings or prevent termination.

## Requirements

### Functional Requirements

- **FR-001**: Users MUST be able to request impact for discovered C/C++ source files, including MFC and C++/CLI files.
- **FR-002**: Native impact MUST trace source-located candidate relations from changed definitions to upstream callers and MFC dispatch entries when unique relation evidence permits.
- **FR-003**: Reports MUST distinguish lexical native candidates from compiler-resolved managed relationships and retain their separate evidence limits.
- **FR-004**: Missing, ambiguous, or sampled native relations MUST reduce completeness and remain explicit; they MUST NOT be reported as resolved calls.
- **FR-005**: Reports MUST include changed paths, caller/target identities, source locations, relationship kinds, bounded counts, and deterministic ordering in JSON and Markdown.
- **FR-006**: Mixed-language impact MUST preserve existing C# and Visual Basic behavior and safely reject paths outside the repository or unsupported source types.
- **FR-007**: Default impact MUST remain local, read-only, source-only for native analysis, and free of new runtime dependencies.

### Evidence Limits

Native trace edges inherit the lexical limitations of the MFC message-path analyzer: macro expansion, active preprocessor branches, overload binding, unqualified calls, runtime dispatch, and C++/CLI-to-C# linkage are not resolved. Native results are candidates for review, not confirmed runtime impact or exhaustive dependency graphs.

## Success Criteria

- **SC-001**: A changed Brownfield Bug Lab MFC view reports the command-map-to-handler path with exact source-relative locations and candidate evidence labels.
- **SC-002**: A changed bridge target reports the upstream qualified call and mapped MFC handler when fixture relations uniquely identify them.
- **SC-003**: Missing, ambiguous, cyclic, repeated, and bounded-sample cases produce deterministic output, explicit completeness limits, and no false resolved native calls.
- **SC-004**: Mixed C#/C++/C++CLI requests retain managed semantic findings and clearly separate them from native candidates in JSON and Markdown.
- **SC-005**: Existing managed-only impact fixtures continue to produce their prior findings and completeness behavior.

## Assumptions

- Native impact is a source-review aid and does not promise compiler-equivalent or runtime-complete impact analysis.
- The existing native flow candidate records are the supported source of native edges; broader C++ parsing and new language toolchains are outside this feature.
- Users can request JSON or Markdown impact output and may select more than one changed file.
