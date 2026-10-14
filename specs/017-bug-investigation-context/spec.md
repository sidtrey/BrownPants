# Feature Specification: Bug Investigation Context

**Feature Branch**: `017-bug-investigation-context`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User wants BrownPants to help make bug reports workable with spec-driven development by orienting investigation in a brownfield repository.

## User Scenarios & Testing

### User Story 1 - Turn a bug report into repository leads (Priority: P1)

A developer has observed-versus-expected behavior, reproduction notes, and clues such as an error message, symbol, or path. They want a repeatable report of matching repository locations that helps scope investigation and write a bug-fix specification.

**Why this priority**: Bug reports often lack repository context to turn directly into a bounded, testable change. Useful leads reduce initial search without pretending to establish cause.

**Independent Test**: Inspect a small temporary repository with clues matching a source symbol, error phrase, path, and test file. Verify that each clue has source-located matches and stable ordering.

**Acceptance Scenarios**:

1. **Given** one or more explicit clues from a bug report, **When** the developer requests investigation context, **Then** the report lists matching source locations with the corresponding clue and match kind.
2. **Given** a repository with likely test files, **When** the developer requests investigation context, **Then** test-path matches are clearly distinguished from other source matches.
3. **Given** a clue with no repository match, **When** the developer requests investigation context, **Then** the report identifies the unmatched clue and does not invent a likely cause.

### User Story 2 - Use leads safely in an SDD workflow (Priority: P2)

A developer wants to bring investigation evidence into a bug-fix spec and later compare implementation changes against the original clues.

**Why this priority**: Evidence is useful only when its origin and limits remain clear through planning and review.

**Independent Test**: Produce Markdown and JSON reports for the same fixture and verify equivalent deterministic source and test references, including evidence limits.

**Acceptance Scenarios**:

1. **Given** the same repository and clues, **When** the developer requests Markdown or JSON, **Then** both formats expose equivalent evidence and identify each originating clue.
2. **Given** matches inside ignored directories or file paths, **When** the developer requests context, **Then** results follow repository scanning rules and distinguish path matches from source-text matches.

### Edge Cases

- Empty or whitespace-only clues must be rejected with actionable usage guidance.
- A clue may match many files; results must be bounded and indicate omitted matches.
- Clues may contain punctuation, paths, identifiers, or repeated phrases.
- Missing, unreadable, or malformed source files must not prevent other repository evidence from being reported.
- Binary files and files larger than 10 MiB are skipped for content matching and reported as bounded read issues; their paths can still match clues.
- A source-text match does not prove reachability, causality, or that observed behavior is a defect.

## Requirements

### Functional Requirements

- **FR-001**: Developers MUST be able to provide one or more explicit clues from a bug report and request repository investigation context.
- **FR-002**: The report MUST identify repository-relative locations that match each clue in file paths or source text, with match kind and source line when applicable.
- **FR-003**: The report MUST identify test-path matches distinctly from other source matches and MUST NOT claim that a matching test reproduces or covers the bug unless that is directly established.
- **FR-004**: Developers MUST be able to obtain Markdown and JSON results with equivalent deterministic evidence.
- **FR-005**: Results MUST be bounded, stable across repeated scans, and consistent with the repository's ignored-directory and symlink rules.
- **FR-006**: The report MUST omit matched source-line contents while retaining clue, path, line, match-kind, and test-candidate metadata.
- **FR-007**: The report MUST state that matches are investigation leads and do not establish cause, behavior, test coverage, or a correct fix.
- **FR-008**: Investigation MUST remain read-only and MUST NOT execute source code, tests, builds, or network requests.
- **FR-009**: The command MUST bound clue count, clue length, match samples, and file content reads, and MUST report files skipped because they cannot be safely searched.

### Key Entities

- **Bug clue**: User-supplied identifier, error phrase, or path fragment to locate in a repository.
- **Investigation match**: A repository-relative location tied to a clue and match kind.
- **Test candidate**: A matching location in a path identified by repository test conventions, not proof of regression coverage.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In fixtures with known clue matches, every expected matching location is reported with a repository-relative path and source line where applicable.
- **SC-002**: In fixtures without matches, the report identifies unmatched clues and contains no fabricated locations or causes.
- **SC-003**: Markdown and JSON reports for identical inputs expose the same ordered match records.
- **SC-004**: Repeated runs on unchanged repositories produce byte-identical output.
- **SC-005**: Investigation runs without building or executing repository code and without network access.
- **SC-006**: Binary and oversized files are skipped for content matching with stable diagnostics while their path matches remain available.

## Assumptions

- Developers supply useful clues from observed and expected behavior and available reproduction details; BrownPants does not infer these from source.
- Initial matching uses case-insensitive literal text matches and does not attempt probabilistic relevance ranking or automatic root-cause claims.
- All languages use the same literal matching rules so clue results remain comparable across mixed repositories.
- SDD engines can consume Markdown or JSON without a BrownPants-specific lifecycle adapter.
