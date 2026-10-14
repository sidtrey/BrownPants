# Feature Specification: Spec Kit Bug Assessment Evidence

**Feature Branch**: `018-speckit-bug-evidence`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Add an optional BrownPants bridge to Spec Kit's bug-assessment flow so report clues can point developers to repository locations.

## User Scenarios & Testing

### User Story 1 - Find repository leads during bug assessment (Priority: P1)

A developer uses Spec Kit's bug assessment command and has pasted an error, identifier, or path from the report. They want BrownPants to locate related files and likely test paths before the assessment is written.

**Why this priority**: A bounded map of relevant locations helps turn a bug report into an evidence-based spec while keeping root-cause analysis with the developer and agent.

**Independent Test**: Install both extensions in a temporary Spec Kit project, invoke bug assessment with distinctive pasted clues, and verify the assessment directory receives a report with matching repository paths and lines.

**Acceptance Scenarios**:

1. **Given** the official Spec Kit bug extension and BrownPants are installed, **When** the developer accepts the optional hook during bug assessment with pasted report text, **Then** it produces a bounded investigation report under that bug's directory.
2. **Given** matching source and test paths, **When** the report is produced, **Then** it preserves each clue and location while identifying test paths only as candidates.
3. **Given** no clue matches, **When** the report is produced, **Then** it states that no repository matches were found and does not infer a cause.

### User Story 2 - Preserve the existing Spec Kit bug workflow (Priority: P1)

A developer wants BrownPants evidence to support the existing assessment, fix, and test steps without replacing or delaying the official workflow when BrownPants is unavailable.

**Why this priority**: The adapter should complement Spec Kit and remain safe to install in projects that do not use its opt-in bug extension.

**Independent Test**: Install BrownPants without the bug extension and verify ordinary lifecycle hooks remain unchanged; install both and verify the optional pre-assessment hook is registered and can skip cleanly when there is no pasted report text.

**Acceptance Scenarios**:

1. **Given** the bug extension is not installed, **When** BrownPants is installed, **Then** its core lifecycle integration remains usable and no competing bug commands are introduced.
2. **Given** an URL-only report or missing BrownPants executable, **When** the optional hook runs, **Then** it reports that evidence was skipped and allows Spec Kit's assessment and URL policy to continue.
3. **Given** generated evidence, **When** Spec Kit writes its assessment, **Then** the evidence remains a separate navigation aid and does not assert causality or test coverage.

## Edge Cases

- The report contains only URLs; BrownPants must not fetch them or interfere with Spec Kit's URL trust policy.
- The bug directory cannot be determined or written; the hook reports the limitation without blocking assessment.
- Report text contains secrets, personal data, or generic words; the hook must avoid forwarding such text as clues.
- The CLI is absent, clues are invalid, or files are unreadable; report a bounded skip/diagnostic and let the official workflow proceed.
- A clue matches many files; preserve the investigator's stable sample limit and omitted-match count.
- A test-path candidate or literal text match does not prove reproduction, coverage, behavior, or cause.

## Requirements

### Functional Requirements

- **FR-001**: The BrownPants Spec Kit adapter MUST register an optional `before_bug_assess` hook for use when the official Spec Kit bug extension is installed.
- **FR-002**: The hook MUST use only bug report text already present in the active conversation; it MUST NOT retrieve URLs or read remote content.
- **FR-003**: The hook MUST derive a bounded set of distinctive, non-sensitive literal clues and invoke BrownPants investigation against the current repository.
- **FR-004**: The hook MUST save Markdown evidence in the active `.specify/bugs/<slug>/` directory without replacing Spec Kit artifacts or including matched source-line contents.
- **FR-005**: The evidence MUST preserve clue provenance, repository-relative paths, line numbers where available, test-candidate labels, omitted counts, and the investigator's limitations.
- **FR-006**: Hook instructions MUST tell the assessment step to treat the report as navigation leads, verify relevant files directly, and avoid unsupported causal or coverage claims.
- **FR-007**: Missing prerequisites, empty clues, or report-write failures MUST be clearly reported as skipped evidence and MUST NOT prevent the official assessment from continuing.
- **FR-008**: BrownPants MUST retain its existing assess/fix/test ownership boundary and core lifecycle hooks.
- **FR-009**: The adapter documentation and automated checks MUST explain and verify that this hook depends on Spec Kit's optional bug extension.

### Key Entities

- **Bug report clue**: A bounded literal identifier, distinctive message, or path fragment selected from report text already supplied to the assessment.
- **Bug evidence report**: A Markdown set of clue-linked repository locations and limitations saved beside the Spec Kit assessment.
- **Bug directory**: The official Spec Kit directory for one bug slug, `.specify/bugs/<slug>/`.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In an installed Spec Kit project with the bug extension enabled, the adapter exposes one optional `before_bug_assess` hook and a runnable BrownPants command; accepting the hook invokes it before report ingestion.
- **SC-002**: For a fixture report containing known clues, all expected sample paths and source lines appear in the saved evidence report.
- **SC-003**: URL-only, unavailable-tool, and no-match cases allow the official bug assessment flow to proceed with an explicit evidence status.
- **SC-004**: The generated evidence contains no matched source snippets and makes no unsupported cause or regression-coverage claims.
- **SC-005**: Core Spec Kit hooks and existing assessment/fix/test instructions continue to pass their existing integration checks.

## Assumptions

- The feature targets the official Spec Kit bug extension's `before_bug_assess` event and is optional when that extension is absent.
- Spec Kit creates the bug directory and supplies its slug and path to the agent before invoking pre-assessment hooks.
- The CLI's existing deterministic clue scan is sufficient; clue selection stays agent-guided and reports are not ranked probabilistically.
- The user's recent instruction-file framing idea is a separate feature direction; this adapter will not compress or rewrite project instruction files.
