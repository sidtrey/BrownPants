# Feature Specification: Local SDD Review Workbench

**Feature Branch**: `035-sdd-workbench`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Idea queue item 6: give non-developers a visual way to review SDD work and return structured feedback without using an AI-specific CLI or service.

## User Scenarios & Testing

### User Story 1 - Review an active feature visually (Priority: P1)

A product owner, QA tester, or other contributor who does not work in the source code wants to understand a Spec Kit feature's user stories, acceptance criteria, task progress, and recorded verification from a browser page.

**Why this priority**: SDD artifacts are currently Markdown and CLI reports. Non-developers need a focused view of what is proposed, what is complete, and what evidence has actually been recorded.

**Independent Test**: Generate a workbench for a temporary Spec Kit feature, open the resulting static HTML, and verify the summary and original artifact contents are present without a server or network.

**Acceptance Scenarios**:

1. **AC-001** Given one active Spec Kit feature or an explicit feature directory, when a contributor generates the workbench, then it identifies the selected feature and presents its user stories, acceptance scenarios, success criteria, task status, and available verification/evidence state.
2. **AC-002** Given the feature task list, when the workbench summarizes progress, then it distinguishes completed and incomplete tasks and labels recorded checks as evidence entries rather than asserting that a checked box alone proves behavior.
3. **AC-003** Given no active feature or multiple candidates and no explicit selection, when workbench generation runs, then it reports an actionable selection error and does not create an output file.

### User Story 2 - Return feedback without editing source artifacts (Priority: P1)

A reviewer wants to attach a question, concern, or suggestion to a story, criterion, or task and hand that feedback to the feature owner without directly editing the specification.

**Why this priority**: Participation must be possible for people who are not comfortable editing Markdown or source files, while keeping changes reviewable by the feature owner.

**Independent Test**: Add feedback to workbench entries, download the review-notes JSON, and verify its item references and text; confirm the source spec, task list, and evidence ledger remain unchanged.

**Acceptance Scenarios**:

1. **AC-004** Given a displayed feature item, when a reviewer adds feedback and exports notes, then the downloaded JSON identifies the feature, item kind/path, and note text in a documented stable format.
2. **AC-005** Given feedback is exported, when the workbench is used, then it has not modified any source artifact or acceptance-criteria baseline.

### User Story 3 - Use the workbench in restricted environments (Priority: P1)

A contributor needs the report to work when network access and AI-specific CLI/SDK clients are unavailable.

**Why this priority**: Enterprise policy is a key adoption constraint, and a report that depends on a hosted renderer or scripts would defeat the intended use.

**Independent Test**: Inspect generated HTML for network references and external assets, then open it as a local file with network access disabled.

**Acceptance Scenarios**:

1. **AC-006** Given a generated report, when it is opened from disk, then its UI and note export work without a local server, external asset, or network request.
2. **AC-007** Given untrusted Markdown text in an artifact, when the report is generated and opened, then it is displayed as text and cannot inject executable HTML or script into the page.

## Edge Cases

- The active feature pointer is missing, malformed, or points outside the repository.
- The requested feature directory is absent or lacks `spec.md`.
- Optional plan, tasks, verification, or evidence files are missing or malformed.
- Tasks include unchecked boxes, completed boxes, and nested checklist entries.
- A note contains quotes, HTML, script-looking text, Unicode, or newlines.
- The output path exists, is a directory, or traverses a link outside the repository.
- The generated report is opened offline in a supported modern browser.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST generate a visually navigable workbench for one selected Spec Kit feature from its repository artifacts.
- **FR-002**: The workbench MUST show stories, acceptance scenarios, success criteria, task completion state, and available verification/evidence entries with clear provenance.
- **FR-003**: The workbench MUST allow reviewers to add feedback associated with a displayed artifact item and export a JSON handoff without modifying repository source artifacts.
- **FR-004**: The report MUST be a self-contained local HTML file with no server, external assets, network calls, or AI-service dependency.
- **FR-005**: Artifact content MUST be safely encoded as display text; user-provided feedback MUST be serialized safely as JSON.
- **FR-006**: Feature and output paths MUST remain within the selected repository, and an existing output MUST be protected unless overwrite is explicit.
- **FR-007**: Ambiguous feature selection or missing required artifacts MUST produce an actionable error without creating a misleading report.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A user can generate a workbench for the active feature with one documented BrownPants command or explicitly select a feature directory.
- **SC-002**: A reviewer can find feature purpose, acceptance criteria, task completion counts, and recorded verification state from the first page without reading source code.
- **SC-003**: A reviewer can export feedback with stable feature and artifact references, and tests prove source artifacts remain unchanged.
- **SC-004**: Generated HTML contains no external asset, URL fetch, or server dependency and safely renders hostile Markdown examples.
- **SC-005**: The feature passes fixture-based CLI tests and the full Release build/test suite without adding a third-party runtime package.

## Assumptions

- The first workbench targets Spec Kit feature directories; other engines can be added through later adapters after validating their artifact contracts.
- Feedback is a handoff file for a human or agent to review; the workbench does not apply suggestions to canonical specs.
- The report may contain repository paths and feature text, so the user chooses an output location and reviews it before sharing.
- BrownPants CLI is permitted as a local repository utility; no AI-host CLI or SDK is required.
