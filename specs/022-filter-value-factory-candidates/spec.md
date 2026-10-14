# Feature Specification: Filter Value-Returning Factory Candidates

**Feature Branch**: `022-filter-value-factory-candidates`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User direction: Improve BrownPants' own architecture report after self-inspection identified a string-returning path resolver as a factory-method candidate.

## User Scenarios & Testing

### User Story 1 - Trust factory-method leads (Priority: P1)

A developer reviewing a brownfield report needs factory-method candidates to point toward methods that can construct or resolve objects, rather than ordinary helpers that happen to use factory-like names.

**Why this priority**: Candidate noise consumes review time and can misdirect implementation analysis, including BrownPants' own self-inspection.

**Independent Test**: Scan a temporary C# and VB.NET repository with string-returning path helpers and object-returning factory methods; verify only the object factory methods are reported.

**Acceptance Scenarios**:

1. **AC-001** — **Given** a C# or VB.NET string-returning helper named with a `Create`, `Build`, or `Resolve` prefix, **When** BrownPants reports architecture candidates, **Then** it does not label the helper as a factory method solely because of its name.
2. **AC-002** — **Given** a C# or VB.NET `Create`, `Build`, or `Resolve` method returning an object type, **When** BrownPants reports architecture candidates, **Then** the method remains visible as a factory-method candidate with its source location.

## Edge Cases

- Nullable string return annotations remain classified as string returns.
- Existing factory method candidates in C# and VB.NET remain discoverable.
- Methods returning `void` or VB `Sub` remain excluded.

## Requirements

### Functional Requirements

- **FR-001**: Factory-method candidates MUST exclude string-returning helpers when the only factory evidence is a `Create`, `Build`, or `Resolve` name prefix.
- **FR-002**: Object-returning factory methods MUST remain candidate signals with source path and line information in Markdown and JSON.
- **FR-003**: The same classification MUST apply to C# and Visual Basic source.
- **FR-004**: Reports MUST continue to label syntax-derived architecture signals as candidates, not proven runtime relationships.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Temporary-repository fixtures show zero factory-method findings for the string path helpers in both languages.
- **SC-002**: The same fixtures show source-located factory-method findings for object-returning methods in both languages.
- **SC-003**: BrownPants' report on this repository no longer identifies `ResolveCustomActiveWorkItemPath` as a factory-method candidate.

## Assumptions

- The targeted false positive is caused by a string-returning path helper; this feature narrows that case while preserving the repository's existing factory detection patterns.
- Other broad candidate categories remain heuristic and require source review.
