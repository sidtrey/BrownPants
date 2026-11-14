# Feature Specification: Source Work Marker Evidence

**Feature Branch**: `076-work-marker-evidence`
**Created**: 2026-11-02
**Status**: Implemented
**Input**: BrownPants-assisted FullMetalFile inspection found an explicitly stubbed archive importer only after a separate TODO search.

## User Scenarios & Testing

### User Story 1 - Find unfinished source work (Priority: P1)

A maintainer inspecting a repository can see source locations marked TODO, FIXME, HACK, or XXX, so unfinished work can be prioritized alongside project, architecture, and test evidence.

**Independent Test**: Scan a small mixed-language repository containing work markers, similarly named identifiers, and marker words in documentation; verify only bounded source marker candidates are reported with stable paths and line numbers.

**Acceptance Scenarios**:

1. **AC-001** Given source files contain TODO, FIXME, HACK, or XXX markers, when a maintainer inspects the repository, then each candidate includes its marker kind, relative path, and one-based line number.
2. **AC-002** Given a source file contains multiple markers on one or more lines, when inspected, then all marker occurrences are counted and samples are sorted deterministically.
3. **AC-003** Given generated directories or documentation contain marker words, when inspected, then generated directories remain excluded and documentation is outside source marker scope.
4. **AC-004** Given source files exceed per-file or aggregate analysis limits, when inspected, then output remains bounded and reports omitted evidence.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST detect case-insensitive TODO, FIXME, HACK, and XXX marker tokens in supported source files.
- **FR-002**: Each marker MUST report a repository-relative path, one-based line, and marker kind without copying source line contents into the report.
- **FR-003**: Marker tokens embedded in larger identifiers MUST NOT be reported as standalone markers.
- **FR-004**: Marker analysis MUST respect repository discovery exclusions and MUST NOT inspect documentation or generated directories.
- **FR-005**: Analysis MUST cap per-file and aggregate bytes and report sample omissions deterministically.
- **FR-006**: Markdown and JSON output MUST expose equivalent totals, omission counts, and sorted samples.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Tests verify all four marker kinds, case insensitivity, identifier boundaries, source-only scope, path/line accuracy, and stable ordering.
- **SC-002**: Tests verify files outside the configured size bounds are not read and omission counts are reported.
- **SC-003**: A BrownPants scan of current FullMetalFile source identifies remaining work markers by source path and line without a separate text search.

## Assumptions

- Marker presence is navigation evidence, not proof that a feature is incomplete or a defect exists.
- Comments and string literals in source files are both candidates; syntax-specific comment parsing is outside this feature.
- Supported source extensions are limited to languages already represented by BrownPants source inventory.
- Reports contain no source snippets, limiting accidental disclosure of repository content.
