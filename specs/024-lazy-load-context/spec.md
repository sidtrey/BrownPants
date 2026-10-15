# Feature Specification: On-Demand Instruction Sections

**Feature Branch**: `024-lazy-load-context`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Let users retrieve only the selected sections of canonical instruction files on demand so an agent can load relevant guidance without receiving every instruction file up front. Keep section lookup deterministic, safe, traceable to its source, and usable by any SDD engine; do not promise host context or token savings."

## User Scenarios & Testing

### User Story 1 - Retrieve a selected instruction section (Priority: P1)

An agent or contributor has found a relevant instruction file and heading. They request that section and receive its text with enough source identity to verify where it came from. This gives hosts and SDD workflows a way to fetch specific canonical guidance when needed.

**Why this priority**: Returning selected canonical text is the core user value; an index without retrieval cannot support demand-loaded guidance.

**Independent Test**: In a temporary repository, request a known heading from a supported instruction file and verify the result contains precisely that section's content and source identity.

**Acceptance Scenarios**:

1. **AC-001** Given a readable supported instruction file with Markdown headings, when a user requests one section by its stable anchor, then the result contains that heading and its body through (but excluding) the next heading of the same or higher level, including nested subsections.
2. **AC-002** Given a request for a section, when Markdown or JSON output is selected, then output is deterministic and identifies the repository-relative source path, heading, anchor, and selected line range alongside the selected section text.
3. **AC-003** Given headings inside fenced code blocks, when a section is requested, then those fenced examples do not create or terminate sections.
4. **AC-004** Given duplicate headings, when a section is requested by its reported anchor, then each duplicate is uniquely addressable and the requested occurrence is returned.

### User Story 2 - Handle unavailable or unsafe section requests (Priority: P1)

A user receives a clear error when a source or heading cannot be safely read, instead of receiving unrelated content or reading outside the repository.

**Why this priority**: The retrieval command must not silently return the wrong guidance or cross the repository boundary.

**Independent Test**: Request missing sources, missing headings, traversal paths, and unreadable or oversized files in temporary repositories and verify actionable errors without section content.

**Acceptance Scenarios**:

1. **AC-005** Given an unknown source or anchor, when the user requests it, then the command exits unsuccessfully with a concise diagnostic and does not return a different section or the entire source.
2. **AC-006** Given a source path that escapes the repository or resolves through a disallowed reparse point, when requested, then the command refuses the read and identifies the unsafe source request.
3. **AC-007** Given invalid arguments or an unsupported output format, when invoked, then the command reports usage or validation details and returns a nonzero status.

## Edge Cases

- A file has no Markdown headings, uses CRLF or LF, or ends without a newline.
- Heading anchors repeat, contain punctuation, non-ASCII characters, or explicit trailing `#` characters.
- A heading is present only inside a fenced code block.
- An instruction source is missing, unreadable, unsupported encoding, oversized, or linked through a reparse point.
- A requested section is the final section or contains deeper nested headings.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST let a user request a single section from a supported instruction source by repository-relative path and section anchor.
- **FR-002**: The system MUST return the selected heading and body up to the next Markdown heading at the same or higher level, retaining nested subsections.
- **FR-003**: The system MUST expose output in Markdown and JSON with stable source path, heading, anchor, line-range, and content fields.
- **FR-004**: The system MUST identify headings only in Markdown text outside fenced code blocks and MUST make duplicate headings uniquely addressable using the same anchor rules shown by instruction inventory.
- **FR-005**: The system MUST reject missing or unreadable sources, unknown anchors, invalid arguments, and unsafe paths without falling back to full-file output.
- **FR-006**: The system MUST use the existing supported-instruction-source discovery and repository safety boundaries; it MUST NOT execute project code, access the network, or modify canonical instruction files.
- **FR-007**: The system MUST explain that it returns content on request but cannot control whether an AI host preloads other instructions or guarantee context-window or token-cost savings.

## Success Criteria

### Measurable Outcomes

- **SC-001**: For every supported fixture heading, a request returns only the requested section and its nested content, with no adjacent peer section text.
- **SC-002**: All valid source and section requests produce byte-for-byte stable output across repeated runs with unchanged files and options.
- **SC-003**: Every invalid, missing, unreadable, or unsafe request fails without returning instruction-section content from another source.
- **SC-004**: Markdown and JSON outputs identify the same source, heading, anchor, line range, and selected text.
- **SC-005**: User-facing help and SDD guidance state that host behavior determines whether other instruction files remain loaded and make no guaranteed token-savings claim.

## Assumptions

- The existing `instructions` inventory remains the canonical way to discover supported instruction files and their section anchors.
- The caller or host selects the relevant source and anchor; automatic relevance ranking, semantic summarization, and host-specific model/context controls are outside this feature.
- Markdown section extraction is a deterministic navigation operation, not a claim that the selected rule is complete, conflict-free, or sufficient for a task.
- Hosts may preload instruction files independently. BrownPants can provide a targeted retrieval interface but cannot change that host behavior by itself.
