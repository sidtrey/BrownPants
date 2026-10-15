# Feature Specification: MFC Message Path Evidence

**Feature Branch**: `054-mfc-message-path-evidence`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Brownfield review found that MFC message-map macros and native calls are inventoried separately, so a reviewer cannot follow a UI command to its handler and onward C++/CLI bridge method.

## User Scenarios & Testing

### User Story 1 - Trace an MFC command into source methods (Priority: P1)

A maintainer investigating a mixed MFC and managed repository needs to find which handler an MFC command names and which explicitly qualified native or C++/CLI methods that handler calls. They need source locations to review the path without treating lexical matches as compiler-resolved edges.

**Why this priority**: MFC clients often enter managed business workflows through message maps and interop bridges. Isolated pattern hits hide the path QA and developers need to assess.

**Independent Test**: Scan a temporary mixed C++ repository with an MFC message map, a handler definition, a qualified call into a C++/CLI bridge in another file, and comment/string/ambiguous decoys. Compare JSON and Markdown paths, lines, statuses, and limits with a fixed answer key.

**Acceptance Scenarios**:

1. **AC-001** Given an MFC command-map entry naming a class method, when inspection runs, then the report links the map entry to exactly one matching source definition with both locations and the command identifier.
2. **AC-002** Given a mapped handler containing an explicitly class-qualified call, when a unique matching method definition exists in the scanned C++/C++CLI source, then the report links the call site to that definition across files as a native-flow candidate.
3. **AC-003** Given missing or multiple matching definitions, when inspection runs, then the relation is marked unresolved or ambiguous and is not represented as a resolved semantic call.
4. **AC-004** Given matching text only in comments, string literals, or raw strings, when inspection runs, then no message-path relation is emitted from that text.
5. **AC-005** Given a mixed repository, when JSON and Markdown reports are generated repeatedly, then native-flow results have stable ordering, source-relative locations, bounded samples, and explicit lexical-analysis limits without building, evaluating, executing, accessing the network, or modifying inspected sources.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST recognize supported MFC message-map entries and retain their command/event identifier, handler type, and handler name.
- **FR-002**: BrownPants MUST link a message-map entry to a unique source method definition using class and method identity; it MUST preserve mapping and definition locations.
- **FR-003**: BrownPants MUST find explicitly class-qualified calls within linked handlers and link them to a unique matching source definition across scanned C++ and C++/CLI files.
- **FR-004**: Every relationship MUST be labeled as a lexical native-flow candidate; the report MUST NOT claim compiler binding, overload selection, runtime dispatch, or a managed call edge.
- **FR-005**: Missing and duplicate targets MUST remain explicit and MUST NOT be silently selected.
- **FR-006**: Markdown and JSON output MUST expose stable relation identity, source and target paths/lines, symbol names, and bounded coverage counts.
- **FR-007**: Default inspection MUST remain source-only, deterministic, bounded, read-only, and free of new runtime package dependencies.

### Evidence Limits

The analyzer recognizes a bounded set of MFC message-map macros and C++ method forms. It does not parse the complete C++ language, expand macros, apply preprocessor conditions, disambiguate overloads, follow unqualified calls, prove C++/CLI-to-C# project linkage, or infer runtime behavior. All native flow edges remain candidates for source review.

## Success Criteria

- **SC-001**: The Brownfield Bug Lab produces the expected two-link chain from `ON_COMMAND` to `OnImportBatch` to `COrderManagedBridge::ImportSelectedOrders`, with exact locations and candidate status.
- **SC-002**: Fixtures cover supported MFC command macros, cross-file definitions, missing and ambiguous targets, and comment/string/raw-string decoys; all expected relations are found and all decoys are excluded.
- **SC-003**: JSON and Markdown outputs are deterministic and bounded, and explicitly report unresolved/ambiguous relation counts and lexical limits.
- **SC-004**: Existing managed semantic call results and repositories without MFC retain their current behavior.

## Assumptions

- Native candidates belong in general repository inspection and the engine-neutral evidence handoff so any SDD engine can consume them.
- C++ source remains lexical-only; this feature does not add a native compiler dependency or broaden the existing C#/VB semantic impact command.
