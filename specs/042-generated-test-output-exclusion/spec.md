# Feature Specification: Generated Test Output Exclusion

**Feature Branch**: `042-generated-test-output-exclusion`

**Created**: 2026-10-15

**Status**: Draft

**Input**: FIFO idea 18: repository inventories include hundreds of generated files under `TestResults/`; skip conventional test-result and build artifact directories by default while retaining an explicit way to inspect user-authored fixtures or reports in those directories.

## User Scenarios & Testing

### User Story 1 - Keep default inventories focused (Priority: P1)

A developer runs BrownPants on a repository and wants source inventory to exclude bulky generated test/build output without hiding authored source or fixtures elsewhere.

**Independent Test**: Discover files in a temporary repository containing common output directories, ordinary source, and nested user fixtures.

**Acceptance Scenarios**:

1. **AC-001** Given a repository containing `TestResults`, `test-results`, `testresults`, `bin`, `obj`, `coverage`, and `artifacts` directories, when its root is inspected, then files under those conventional generated-output directories are omitted.
2. **AC-002** Given authored source and fixtures outside those output directories, when the repository is inspected, then they remain discoverable.

### User Story 2 - Inspect selected output when needed (Priority: P1)

A maintainer needs to review a test report or fixture stored under a normally excluded directory.

**Independent Test**: Select the excluded directory itself as the repository root and verify its files are discoverable and reportable.

**Acceptance Scenarios**:

1. **AC-003** Given a path to a `TestResults` directory, when that path is explicitly inspected as the root, then its files are included.
2. **AC-004** Given an excluded directory is a symbolic link or an unreadable path, when inventory runs, then existing safe traversal and error behavior is preserved.

## Edge Cases

- Directory matching is case-insensitive and independent of host OS.
- The selected root itself is never excluded solely because its basename matches a generated-output convention.
- Authored fixtures under an excluded child directory are omitted from a parent scan but remain available when that directory is explicitly selected as root.
- Existing ignore rules for package, build, VCS, and frontend outputs remain in effect.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST skip `TestResults`, `test-results`, and `testresults` child directories during default repository discovery.
- **FR-002**: BrownPants MUST preserve existing exclusions for `bin`, `obj`, `coverage`, and `artifacts`.
- **FR-003**: BrownPants MUST continue scanning when the selected repository root itself has an excluded directory name.
- **FR-004**: BrownPants MUST not exclude normal `tests`, `fixtures`, or user-authored source directories by broad name matching.
- **FR-005**: The feature MUST be deterministic and must not delete or modify generated/user-authored files.

### Key Entities

- **Generated-output directory**: A conventional child folder whose content is omitted from a parent repository inventory by default.
- **Explicit scan root**: The directory provided by the user; its own name does not trigger child-directory exclusion.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Fixture tests omit all configured output directories and retain authored source/fixture files outside them.
- **SC-002**: Scanning an excluded directory directly includes its contents.
- **SC-003**: Repeated file discovery returns the same ordered paths and does not modify the fixture tree.
- **SC-004**: Existing repository scanner and full solution tests pass without changing other ignore behavior.

## Assumptions

- `artifacts`, `bin`, `obj`, and `coverage` are already excluded; this feature adds the three conventional test-result directory spellings.
- Explicit selection is achieved by using the desired directory as the `inspect` path; no new include flag is necessary.
