# Feature Specification: Configure Active Work Items for Custom SDD Engines

**Feature Branch**: `010-custom-sdd-active-work-item`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User goal: make BrownPants a useful Spec Kit brownfield companion that can also serve other SDD engines and codebases.

## User Scenarios & Testing

### User Story 1 - Identify active work in a custom SDD engine (Priority: P1)

A maintainer configures an SDD workflow not built into BrownPants and wants its selected feature or change to appear in the repository report alongside built-in workflows.

**Why this priority**: Custom engines can currently provide artifact roots and counts, but their selected work item is omitted from context reports.

**Independent Test**: Inspect a temporary repository with a custom workflow declaration and active artifact directory; verify matching path and evidence in JSON and Markdown.

**Acceptance Scenarios**:

1. **Given** a valid custom workflow declaration whose active path is inside its artifact roots and has scanned files, **When** BrownPants inspects the repository, **Then** both report formats show that path with custom-declaration evidence.
2. **Given** an existing workflow declaration without an active path, **When** BrownPants inspects the repository, **Then** its existing counts and samples are preserved and the active selection remains null.

### User Story 2 - Reject unsupported custom selections safely (Priority: P2)

A maintainer wants an invalid, unsafe, outside-root, or stale custom path diagnosed without BrownPants reading outside its discovered inventory or claiming the path is active.

**Why this priority**: Incorrect context may send an agent to the wrong work item, while configuration must remain declarative and bounded.

**Independent Test**: Inspect fixtures with traversal, outside-root, and missing-artifact selections; verify null selection and stable diagnostics.

**Acceptance Scenarios**:

1. **Given** an unsafe or invalid active path, **When** BrownPants inspects the repository, **Then** it reports a configuration issue and no active work item.
2. **Given** a safe path outside the workflow's artifact roots or without scanned files, **When** BrownPants inspects the repository, **Then** it reports a configuration issue and no active work item.

## Edge Cases

- Workflow declaration omits the optional field or supplies null, a non-string value, an empty string, or duplicate JSON property.
- Active path has traversal, absolute, drive, wildcard, empty, dot, or dot-dot segments.
- Active path is a valid directory but is not within any declared artifact root.
- Active directory exists but contains no files represented in the scanner inventory.
- Path uses Windows separators or contains spaces; reports must normalize separators and preserve safe names.

## Requirements

### Functional Requirements

- **FR-001**: A custom workflow MAY declare an optional `activeWorkItemPath` repository-relative directory.
- **FR-002**: BrownPants MUST accept a declared path only when it is safe, lies beneath one of that workflow's artifact roots, and has at least one descendant in the scanner's existing file inventory.
- **FR-003**: A valid custom selection MUST populate `ActiveWorkItemPath` and the `CustomWorkflowDeclaration` evidence label in Markdown and JSON.
- **FR-004**: Invalid or stale custom selections MUST leave both active work item fields null and add a deterministic configuration diagnostic without echoing unsafe path values.
- **FR-005**: Omitting `activeWorkItemPath` MUST preserve schema version 1 behavior, existing workflow counts, samples, and compatibility with current declarations.
- **FR-006**: Custom selection MUST remain declarative and MUST NOT trigger external reads, network access, project evaluation, or tool execution.

## Key Entities

- **Custom workflow definition**: Existing workflow declaration with optional active work item directory.
- **Active work item**: Existing report entity populated from a validated custom declaration, identified by `CustomWorkflowDeclaration` evidence.
- **Configuration issue**: Existing stable diagnostic with path, code, and safe message.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Valid custom active paths appear identically in Markdown and JSON with the expected evidence label.
- **SC-002**: 100% of unsafe, outside-root, and stale custom paths in fixtures remain unset and produce deterministic diagnostics.
- **SC-003**: All existing custom workflow fixtures without the new property retain their previous inventory values.
- **SC-004**: Repeated scans of identical custom workflow fixtures produce byte-identical reports.

## Assumptions

- The repository maintainer updates the declarative field when the custom engine's active work item changes.
- The configured path identifies a directory under the workflow's declared artifact roots; a custom engine's internal selection file is not parsed.
- Existing root safety rules define valid path segments and separators.
