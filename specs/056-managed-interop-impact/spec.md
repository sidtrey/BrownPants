# Feature Specification: Managed Interop Impact

**Feature Branch**: `056-managed-interop-impact`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Extend brownfield call evidence across C++/CLI bridge methods and their C# or Visual Basic managed targets, including upstream MFC impact.

## User Scenarios & Testing

### User Story 1 - See the managed target behind an interop bridge (Priority: P1)

A maintainer reviewing an MFC/C++/CLI client needs to see which managed method a bridge calls and where that method is defined, so the handoff into the managed system is reviewable.

**Why this priority**: Existing native flow stops at the C++/CLI bridge. In a mixed .NET/MFC system, that leaves the main business-code boundary invisible.

**Independent Test**: Inspect a mixed-language fixture with a typed C++/CLI handle calling a uniquely declared C# method; compare the reported bridge call and target definition against source paths and lines.

**Acceptance Scenarios**:

1. **AC-001** **Given** a C++/CLI method with an explicitly typed managed handle calling a uniquely declared C# or VB method, **When** inspection runs, **Then** it reports the call site and managed target definition as a native lexical candidate with both locations.
2. **AC-002** **Given** a mixed MFC, C++/CLI, and managed fixture, **When** a managed target file is selected for impact, **Then** impact traces the candidate path through the bridge and MFC dispatch when each link is unique.
3. **AC-003** **Given** multiple matching managed declarations or no matching declaration, **When** inspection runs, **Then** it reports ambiguity or unresolved status and does not select a target as unique.

### User Story 2 - Keep interop evidence safe and useful to SDD tools (Priority: P2)

A developer or SDD agent needs deterministic source-located interop findings in Markdown and JSON, with evidence strength and limits visible so candidate matches are not mistaken for compiler binding or runtime behavior.

**Why this priority**: The same boundary evidence should help code review, QA, and engine-neutral task handoff without overstating certainty.

**Independent Test**: Repeat inspection and focused evidence on fixtures with comments, literals, wrong receiver types, overloaded methods, and truncated samples; compare stable output and evidence labels.

**Acceptance Scenarios**:

1. **AC-004** **Given** decoy calls in comments/strings or calls on handles whose managed type does not match the target declaration, **When** analysis runs, **Then** no cross-language relation is emitted for those decoys.
2. **AC-005** **Given** an unresolved, ambiguous, or sampled cross-language edge, **When** impact or evidence is requested, **Then** the limitation remains explicit and the report does not imply no impact.
3. **AC-006** **Given** repeated scans of unchanged sources, **When** Markdown and JSON are produced, **Then** candidate identities, locations, ordering, and counts remain stable without building or modifying the inspected repository.

### Edge Cases

- Same-named managed types in multiple namespaces or languages.
- Generic and partial managed types, overloaded methods, and duplicate declarations.
- Managed handle declaration has no supported local type syntax or call uses a different receiver form.
- C# or VB method index reaches its bound, or native relations are sampled.
- Changed managed file has managed Roslyn callers and native candidate callers simultaneously.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST identify supported C++/CLI managed-handle declarations and calls made through those handles.
- **FR-002**: BrownPants MUST match handle type and method name against C# and Visual Basic source declarations and retain source locations for each candidate target.
- **FR-003**: A unique source match MUST be labeled a native lexical candidate; multiple and missing matches MUST remain ambiguous or unresolved.
- **FR-004**: Inventory and engine-neutral evidence MUST expose bridge-call relation kind, evidence classification, status, source/target identities, locations, candidate count, and candidate target locations when non-unique.
- **FR-005**: Impact for a changed managed target MUST trace incoming C++/CLI candidate links and any supported upstream native/MFC candidate links, separately from Roslyn-resolved managed callers.
- **FR-006**: Comments, strings, unsupported receiver types, and same-named unrelated methods MUST NOT be represented as unique cross-language links.
- **FR-007**: Output MUST be deterministic, bounded, source-only, read-only, and must not build projects or introduce runtime package dependencies.
- **FR-008**: Reports MUST state that matching source declarations does not prove project references, compiler binding, overload choice, call execution, or runtime impact.

## Success Criteria

- **SC-001**: The Brownfield Bug Lab reports a source-located chain from `ID_ORDER_IMPORT_BATCH` through `COrderMfcView::OnImportBatch` and `COrderManagedBridge::ImportSelectedOrders` to the managed `OrderImportWorkflow<T>.ImportBatch` declaration.
- **SC-002**: Unique managed targets include their exact source-relative path and line; overloads, duplicate types, and missing targets are not silently resolved.
- **SC-003**: Focused impact for the changed managed workflow reports the native bridge and MFC command as separate lexical candidates alongside any managed semantic findings.
- **SC-004**: Fixtures exclude comment/string/wrong-type decoys, preserve candidate-target locations and completeness limits, and produce byte-stable JSON/Markdown across repeated scans.
- **SC-005**: Existing native-only and managed-only impact behavior remains intact; no source writes, project evaluation, or build is required for cross-language candidates.

## Assumptions

- The supported v1 bridge shape is a C++/CLI method body with a locally declared managed handle and an instance call through that handle.
- Type/method matching is source-based and may remain ambiguous; BrownPants does not invoke a C++ or .NET compiler to prove interop.
- C# and VB targets are in discovered source files, whether or not their projects can be built by the current host.
