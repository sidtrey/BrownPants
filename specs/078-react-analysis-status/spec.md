# Feature Specification: Explicit React Candidate Analysis Status

**Feature Branch**: `078-react-analysis-status`

**Created**: 2026-11-06

**Status**: Implemented

**Input**: BrownPants FullMetalFile inspection showed a detected React package and TSX declarations alongside zero component and hook-call candidates, without stating that parser-backed candidate analysis was not requested.

## User Scenarios & Testing

### User Story 1 - Understand whether React candidates were analyzed (Priority: P1)

As a maintainer inspecting a React repository, I want BrownPants to say whether component and hook candidates were analyzed, so I can distinguish an opt-in parser that did not run from a verified scan that found no candidates.

**Why this priority**: An unexplained zero can be read as “there are no React components,” although the default scan intentionally skips parser-backed candidate analysis.

**Independent Test**: Inspect a fixture with a React manifest and TSX component both with and without parser verification, then compare the JSON status and Markdown explanation.

**Acceptance Scenarios**:

1. **AC-001**: **Given** a React package and TSX source, **when** default inspection runs without parser verification, **then** JSON identifies candidate analysis as `not-requested` and Markdown explains how to request it without presenting zero candidates as a completed analysis.
2. **AC-002**: **Given** parser verification completes successfully, **when** BrownPants reports React source evidence, **then** JSON and Markdown identify the verified status and report the candidate counts and source locations.
3. **AC-003**: **Given** parser verification is unavailable or fails, **when** BrownPants reports React source evidence, **then** its status distinguishes that outcome from both `not-requested` and `verified`.

## Edge Cases

- A verified React scan finds zero component or hook-call candidates; the report states that the scan completed and found none.
- A parser is requested but the workspace has no eligible JavaScript/TypeScript source files.
- A React package is detected but parser verification is unavailable.
- Candidate records are omitted by a sample limit; the status remains visible and omission counts remain accurate.

## Requirements

### Functional Requirements

- **FR-001**: React candidate analysis MUST expose a status that distinguishes `not-requested`, `verified`, `partial`, `unavailable`, and `failed` outcomes.
- **FR-002**: Default inspection MUST remain free of Node invocation and MUST label React source candidates as not requested.
- **FR-003**: Markdown MUST explain that parser verification is needed to produce component and hook-call candidates when it was not requested.
- **FR-004**: JSON and Markdown MUST report a consistent React candidate-analysis status.
- **FR-005**: Component and hook counts MUST be presented as analyzed counts only when parser verification completed or partially completed; unavailable counts MUST NOT be mistaken for zero findings.
- **FR-006**: Existing opt-in parser behavior, React candidate locations, and bounded sample/omission counts MUST remain unchanged.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In default inspection of a React package, both JSON and Markdown identify candidate analysis as not requested and provide the parser-verification option.
- **SC-002**: In parser-verified inspection, JSON and Markdown use the same verified/partial/unavailable/failed status as the syntax verifier.
- **SC-003**: For a verified fixture with a component and hook call, both output formats preserve their source locations; for a verified fixture with none, both state that zero candidates were found after analysis.
- **SC-004**: Default inspection does not invoke Node or execute project source.

## Assumptions

- Compiler-backed React source recognition remains opt-in to preserve the safe default scan.
- Function component and hook-call results remain syntax candidates and do not prove runtime behavior.
- The existing TypeScript parser status is the authoritative source for candidate-analysis completion status.
