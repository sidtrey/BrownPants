# Feature Specification: C# Consumption of C++/CLI Libraries

**Feature Branch**: `feature/074-csharp-cli-consumption-impl`
**Created**: 2026-11-12
**Status**: Draft
**Input**: Brownfield evidence gap recorded in IDEA-QUEUE item 40: start at a C# call site and navigate through a referenced C++/CLI wrapper to native calls where source is available.

## User Scenarios & Testing

### User Story 1 - Follow a C# call into a mixed-mode library (Priority: P1)

A maintainer investigating a legacy C# application needs to follow a call to a C++/CLI library and onward to its native implementation, so the boundary and the source locations that may need review are visible from the managed entry point.

**Why this priority**: In brownfield work, the change request and the first relevant call site often live in C#. Existing native-flow evidence starts from MFC or C++/CLI code and can leave that consumer path undiscovered.

**Independent Test**: Scan a fixture containing a C# consumer, an old-style assembly reference with a relative binary path, a corresponding C++/CLI project and wrapper declaration/definition, and a qualified native call. Confirm that focused evidence for the C# source contains the candidate chain and that every link has a source location or an explicit unresolved reason.

**Acceptance Scenarios**:

1. **AC-001** Given a C# call whose receiver type and method match a managed declaration in a referenced C++/CLI source project, when repository evidence is requested, then BrownPants reports a source-located candidate from the C# call to the wrapper declaration and implementation.
2. **AC-002** Given a wrapper implementation that makes a supported qualified native call, when the C# call is followed, then the report includes that native call as a separate candidate edge and preserves all source locations.
3. **AC-003** Given a legacy assembly reference whose binary is missing but whose relative path identifies a C++/CLI project/output candidate, when evidence is requested, then BrownPants reports the reference and source candidate without claiming that the binary resolves or loads.

### User Story 2 - Understand incomplete or uncertain interop paths (Priority: P2)

A developer or SDD agent needs ambiguities, missing references, missing binaries, and unsupported call shapes to remain visible, so a partial source match is not mistaken for compiler or runtime proof.

**Why this priority**: A false claim across a managed/native boundary can send a change to the wrong component. Evidence needs to remain useful while making uncertainty clear.

**Independent Test**: Repeat the fixture with missing and ambiguous wrapper definitions, an unrelated same-named method, a comment/string decoy, an unresolved binary, and a bounded sample; verify statuses, locations, and omission counts in JSON and Markdown.

**Acceptance Scenarios**:

1. **AC-004** Given a same-named method without a matching assembly/project reference or receiver type, when analysis runs, then BrownPants does not present a unique cross-boundary link.
2. **AC-005** Given an ambiguous or missing wrapper implementation, when the consumer is analyzed, then all retained candidate locations or the unresolved reason are reported and the path is marked incomplete.
3. **AC-006** Given comments, string literals, unsupported dynamic calls, or conditional project metadata, when source-only analysis runs, then those constructs are not promoted to verified project or runtime relationships.

## Edge Cases

- A legacy `HintPath` is relative to the consuming project and points to a missing output directory.
- Multiple references resolve to the same assembly name or candidate project.
- A project reference points to a native project whose output metadata is configuration-dependent.
- Wrapper declarations and definitions are in different files, overloaded, inline, or absent.
- C# receiver type is qualified, imported, aliased, constructed locally, or hidden behind `var`/dynamic typing.
- The reference path leaves the repository, contains traversal segments, or targets a symlink/reparse point.
- Project/source samples are truncated or one of the referenced files cannot be read.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST report source-located C# invocation candidates that can be associated with a supported reference to a discovered C++/CLI project or source declaration.
- **FR-002**: BrownPants MUST inspect legacy assembly-reference metadata, including `Reference` identity and relative `HintPath`, and project-reference metadata without evaluating imported build targets.
- **FR-003**: BrownPants MUST link a consumer call to a wrapper only when the available receiver/type, method, and reference evidence agree; it MUST retain all matching target locations when the match is ambiguous.
- **FR-004**: BrownPants MUST continue a supported wrapper path to explicitly qualified native calls using existing native-flow evidence, while keeping each boundary edge separately identified.
- **FR-005**: Focused evidence and changed-path impact MUST support starting from the C# consumer and include match reasons, source paths/lines, coverage, and omitted-sample counts.
- **FR-006**: Missing binaries, source projects, wrapper definitions, or unsupported call forms MUST be reported as unresolved or incomplete evidence rather than silently treated as no impact.
- **FR-007**: Markdown and JSON MUST distinguish syntax/source candidates from compiler-resolved relationships and runtime behavior.
- **FR-008**: The default analysis MUST be deterministic, bounded, local, read-only, and must not build projects, evaluate imported targets, load binaries, or access the network.
- **FR-009**: Reference paths MUST be normalized and constrained to discovered repository content; external or unsafe paths MUST not trigger reads outside the repository.

### Key Entities

- **Managed consumer call**: A C# invocation with source location and receiver/type evidence.
- **Library reference**: A project or assembly reference, including the available identity and path metadata.
- **Wrapper candidate**: A C++/CLI managed method declaration or definition that may correspond to the consumer call.
- **Interop path**: An ordered set of source-backed candidate edges from consumer through wrapper to native call sites, with per-edge status and limits.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All expected consumer, reference, wrapper, and native-call links in the pinned VCSamples scenario are present in a source-anchored evidence report, with no link labeled compiler-resolved.
- **SC-002**: The fixture suite distinguishes unique, ambiguous, unresolved, and unsupported paths for every declared test case, with zero false unique links in its decoy cases.
- **SC-003**: Focused JSON and Markdown identify the same candidate path, source locations, statuses, and completeness limits.
- **SC-004**: Repeated scans of unchanged fixtures produce byte-identical normalized JSON and Markdown, and scanning neither writes to the fixture nor evaluates its projects.
- **SC-005**: Existing managed impact and native-flow test results remain unchanged outside the new consumer-start path.

## Assumptions

- The first supported consumer language is C#; Visual Basic consumer analysis is outside this feature.
- A source-only reference-to-project association is a candidate, even when `HintPath` and project metadata agree; it does not prove that a binary was built from that source.
- Existing C++/CLI declaration and qualified native-call recognition remains the supported wrapper/native syntax boundary.
- The pinned Microsoft VCSamples revision and the repository-local regression fixture provide independent expected paths; their project binaries need not exist or build on the host.
