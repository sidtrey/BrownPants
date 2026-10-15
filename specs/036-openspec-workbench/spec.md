# Feature Specification: OpenSpec Review Workbench

**Feature Branch**: `036-openspec-workbench`

**Created**: 2026-10-14

**Status**: Implemented

**Input**: FIFO item 6: let non-developers review and return feedback on SDD work without requiring an AI-specific CLI or hosted service; extend the feature 035 local workbench to OpenSpec changes.

## User Scenarios & Testing

### User Story 1 - Review an OpenSpec change (Priority: P1)

A product owner, QA tester, or other contributor wants to review an OpenSpec change's proposal, specification deltas, task progress, and design from a local browser page.

**Why this priority**: BrownPants already inventories OpenSpec workflows, but the visual review workbench currently understands only Spec Kit features. This leaves GUI-based participation inconsistent across supported SDD engines.

**Independent Test**: Generate a workbench from a temporary OpenSpec repository and verify proposal, requirement/scenario, task, and design information in self-contained HTML.

**Acceptance Scenarios**:

1. **AC-001** Given `workbench` is invoked without an engine option, when the repository has an active Spec Kit feature, then the existing Spec Kit selection and report behavior remain unchanged.
2. **AC-002** Given `--engine openspec`, when exactly one non-archived OpenSpec change exists, then the workbench selects that change; when multiple changes exist, an explicit `--feature` is required and the error lists an actionable selection instruction.
3. **AC-003** Given a selected OpenSpec change, when the workbench is generated, then it presents proposal text, requirement and scenario headings from delta specs, task completion state, and available design text with their artifact paths.
4. **AC-004** Given OpenSpec tasks and review notes, when the report is used, then completed and incomplete tasks are distinguished and notes can be exported against stable change artifact references without modifying source files.

### User Story 2 - Review safely in a restricted environment (Priority: P1)

A reviewer needs to open and annotate the report where network access and AI-specific CLIs are unavailable.

**Independent Test**: Open generated HTML as a local file, export notes, and verify the report has no external assets or network requests and encodes hostile Markdown as text.

**Acceptance Scenarios**:

1. **AC-005** Given a generated OpenSpec workbench, when it is opened from disk, then the page and note export work without a server, network request, external resource, or new runtime package.
2. **AC-006** Given malformed, missing, archived-only, ambiguous, or escaping OpenSpec selection, when generation fails, then the error identifies how to fix selection and no output file is created.
3. **AC-007** Given untrusted text in any OpenSpec artifact, when the report is generated, then it is encoded as display text and cannot inject executable HTML or script.

## Edge Cases

- No `openspec/` tree, only archived changes, or multiple non-archived changes.
- Explicit change path is outside `openspec/changes/`, points into `archive/`, is missing, or lacks required `proposal.md`.
- Optional `design.md`, `tasks.md`, and delta spec files are absent; malformed or oversized files are present.
- Multiple capability delta specs contain the same requirement title.
- Nested task checkboxes and unchecked/completed states are present.
- An artifact contains script-looking HTML, invalid UTF-8, or a symlink/reparse point.
- The report output aliases an input artifact or crosses outside the repository.

## Requirements

### Functional Requirements

- **FR-001**: The existing `workbench` command MUST retain its default Spec Kit behavior and add explicit OpenSpec engine selection.
- **FR-002**: OpenSpec selection MUST choose a single non-archived change deterministically or require an explicit in-repository change path when selection is ambiguous.
- **FR-003**: The report MUST present proposal, delta requirement/scenario, task, and optional design content with repository-relative source paths; raw artifact text MUST remain available for review.
- **FR-004**: Task completion MUST be shown as recorded checklist state, not proof that implementation or verification passed.
- **FR-005**: Reviewers MUST be able to export notes tied to stable change/artifact references; generation and export MUST NOT edit OpenSpec files.
- **FR-006**: OpenSpec input and output paths MUST stay inside the repository, reject reparse-point traversal, bound artifact reads and delta-directory traversal, require valid UTF-8, and protect existing files unless overwrite is explicit.
- **FR-007**: Invalid or ambiguous selection MUST produce an actionable nonzero error without creating output.
- **FR-008**: The generated page MUST be self-contained, offline-capable, and safely encode untrusted artifact text without third-party runtime dependencies.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A reviewer can generate an OpenSpec change report with one command, using automatic unique-change selection or explicit selection.
- **SC-002**: A reviewer can locate the proposal, each parsed requirement/scenario, all task statuses, and available design from the first page with source paths.
- **SC-003**: Exported note references remain unique when different capability files use the same requirement title, and source snapshots remain unchanged.
- **SC-004**: Fixture tests cover active/explicit/ambiguous/archived/invalid selection, output safety, hostile artifact text, and offline HTML behavior.
- **SC-005**: The Release build and complete test suite pass without adding a third-party runtime package.

## Assumptions

- OpenSpec changes live under `openspec/changes/<name>/`; `archive/` is not active work.
- `proposal.md` is required. `design.md`, `tasks.md`, metadata, and delta specs are optional so early-stage and spec-skipping changes can still be reviewed.
- Delta specs follow the documented `### Requirement:` and `#### Scenario:` heading conventions. Unrecognized content remains available in the raw artifact viewer.
- The workbench is a local review handoff, not an OpenSpec validator, archive command, or editor for canonical files.
