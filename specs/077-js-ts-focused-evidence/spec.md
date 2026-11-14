# Feature Specification: JavaScript and TypeScript Focused Evidence

**Feature Branch**: 077-js-ts-focused-evidence
**Created**: 2026-11-06
**Status**: Implemented
**Input**: Allow BrownPants evidence handoffs to focus on changed JavaScript and TypeScript source paths and report bounded syntax-based caller candidates.

## User Scenarios & Testing

### User Story 1 - Focus evidence on changed JavaScript and TypeScript files (Priority: P1)

As a developer working in a React or TypeScript repository, I need BrownPants to accept a changed `.js`, `.jsx`, `.mjs`, `.cjs`, `.ts`, `.tsx`, `.mts`, or `.cts` file so that focused evidence can include source-local findings and supported callers without requiring C# changes.

**Independent Test**: Scan a temporary repository with a TS declaration and a TSX caller, focus evidence on the declaration path, and verify the caller is included with a syntax-candidate reason and explicit evidence limitations.

**Acceptance Scenarios**:

1. **AC-001** Given a discovered JavaScript or TypeScript source path, when supplied with `evidence --changed`, then the path is accepted and reported as found.
2. **AC-002** Given a unique syntax-resolved JavaScript/TypeScript call targets the changed file, when focused evidence is selected, then the caller finding is included with a changed-path syntax-candidate reason and source location.
3. **AC-003** Given a dynamic, unresolved, or ambiguous call cannot be uniquely linked to the changed file, when focused evidence is selected, then BrownPants does not invent a caller target and keeps the syntax evidence limitation visible.
4. **AC-004** Given C#, Visual Basic, native C/C++, and JavaScript/TypeScript paths are mixed, when evidence is focused, then each path is handled by its supported analyzer without unsupported-language errors.
5. **AC-005** Given an unsupported extension, absolute path, traversal path, or missing file is supplied, when evidence is focused, then the CLI rejects it with a specific validation error.

### User Story 2 - Preserve evidence boundaries (Priority: P1)

As a maintainer, I need focused JavaScript/TypeScript impact to remain clearly distinct from compiler-resolved C# impact so evidence consumers do not treat syntax candidates as type-checked relationships.

**Acceptance Scenarios**:

1. **AC-006** Given JavaScript/TypeScript changed-path evidence, when the report is rendered, then it identifies syntax-candidate impact and states that overload binding, types, and runtime dispatch were not resolved.
2. **AC-007** Given existing C#/VB semantic and C/C++ native-flow evidence, when those paths are focused, then their existing behavior and completeness reporting remain unchanged.

## Requirements

- **FR-001**: Focused evidence MUST accept the eight JavaScript/TypeScript extensions already supported by source analysis.
- **FR-002**: Path normalization MUST retain repository-relative, discovered-file, traversal, and root-containment checks.
- **FR-003**: JavaScript/TypeScript findings MUST be selected for a changed file when their source path matches that file.
- **FR-004**: A unique syntax-resolved JavaScript/TypeScript call targeting a changed path MUST add its caller finding with a distinct syntax-candidate match reason.
- **FR-005**: Ambiguous, unresolved, and dynamic relationships MUST NOT be promoted to unique callers.
- **FR-006**: Managed C#/VB and native C/C++ paths MUST continue through their existing call-impact analyzer; JavaScript/TypeScript paths MUST NOT be sent to an analyzer that rejects them.
- **FR-007**: JSON help, CLI help, focus completeness, and limitations MUST describe the supported changed-path language set and syntax evidence boundaries.
- **FR-008**: Tests MUST cover valid JS/TS/TSX paths, unique caller selection, mixed-language paths, unsupported paths, and regressions for existing managed/native paths.

## Success Criteria

- **SC-001**: Focused evidence accepts every supported JS/TS extension and rejects unsafe or unsupported paths.
- **SC-002**: A changed TypeScript target selects a source-located TSX caller only when the analyzer reports a unique syntax-resolved target.
- **SC-003**: Reports do not label JS/TS call candidates compiler-resolved and clearly state omitted semantic guarantees.
- **SC-004**: Existing BrownPants build and tests pass with unchanged C#/VB/native focused-evidence behavior.
- **SC-005**: A focused FullMetalFile React handoff using `--changed FullMetalFile.Client/src/CaptureWorkspace.tsx` succeeds and returns relevant evidence.
- **SC-006**: Spec Kit acceptance evidence validates against the recorded criteria digest.

## Assumptions

- The existing bounded JavaScript/TypeScript analyzer is the evidence source; no package manager, project script, TypeScript compiler, or network access is required.
- Syntax-resolved relationships are candidates and do not prove type correctness, overload binding, runtime dispatch, or execution.
- `--semantic` continues to mean managed MSBuild/Roslyn analysis; it is not required for JavaScript/TypeScript candidate selection.
