# Feature Specification: Local Text Flowcharts

**Feature Branch**: `032-local-text-flowcharts`

**Created**: 2026-10-15

**Status**: Draft

**Input**: Idea queue item 9: provide a security-conscious, local charting option for teams that cannot use hosted flowchart tools.

## User Scenarios & Testing

### User Story 1 - Render a flowchart locally as a PNG (Priority: P1)

A developer or analyst writes a Mermaid flowchart in a Markdown document and wants to create a shareable PNG locally without uploading source to a hosted diagram service or installing a separate renderer.

**Why this priority**: Teams need a usable flowchart path when external diagram sites are blocked or repository content cannot be uploaded.

**Independent Test**: Render a temporary in-repository Markdown file containing branching, merging, and cyclic relationships; verify the PNG signature and dimensions, deterministic pixel output, and explicit diagnostics for unsupported syntax.

**Acceptance Scenarios**:

1. **AC-001** — Given one fenced `brownpants-flow` block using the documented BrownPants-owned node and edge statements, when the user runs `brownpants diagram <repo> <relative-markdown-path> --output <relative-path>`, then BrownPants creates a deterministic PNG by default or an editable native `.drawio` XML file when `--format drawio` is selected, preserving node labels, branch labels, shapes, and edge relationships.
2. **AC-002** — Given a graph with a cycle or shared node, when rendered, then the PNG includes every node and relationship, routes repeated-node edges without recursion or silent omission, and remains within documented image bounds.
3. **AC-003** — Given malformed input, multiple flowchart blocks, or unsupported Mermaid syntax, when rendered, then BrownPants returns a nonzero result with source path and line diagnostics and does not create or replace the output image.
4. **AC-004** — Given a missing input, a path outside the repository, a linked/reparse-point path, or an input or graph exceeding documented limits, when rendered, then BrownPants refuses it without reading outside the repository or writing outside the selected output path.
5. **AC-005** — Given an existing output image, when rendering is requested without `--overwrite`, then BrownPants preserves the image and returns an actionable conflict message; with explicit `--overwrite`, it replaces only the selected in-repository output.
6. **AC-006** — Given a valid flowchart and `--format drawio`, when BrownPants writes the selected output, then the result is well-formed native diagrams.net XML that can be imported by the desktop application and contains no external links, scripts, or image references.

## Requirements

### Functional Requirements

- **FR-001**: The CLI MUST render one explicit `brownpants-flow` fenced block from a Markdown file selected by a repository-relative path into a PNG by default or a native `.drawio` XML file with `--format drawio`, at an explicitly selected in-repository output path.
- **FR-002**: The BrownPants-owned grammar MUST be documented and include `flowchart <id>`, `node <id> <shape> "<label>"` declarations for `start`, `process`, `decision`, and `end` shapes, `edge <from> -> <to>` relationships with optional `label "<text>"`, blank lines, and `#` comments.
- **FR-003**: The .NET renderer MUST deterministically lay out nodes, labels, branch labels, and directed relationships, including cycles and shared nodes, without Mermaid.js, browser automation, native graphics dependencies, or network access. PNG output is rasterized locally; `.drawio` output is native editable XML.
- **FR-004**: PNG output MUST be valid and bounded in pixel dimensions and total pixels; unsupported text glyphs MUST produce an actionable diagnostic rather than being silently corrupted.
- **FR-005**: Unsupported syntax MUST produce a source-path-and-line diagnostic and a nonzero result without creating or replacing an output image.
- **FR-006**: Input and output paths MUST remain inside the repository, reject symbolic links/reparse points, and obey documented source, graph, and image limits.
- **FR-007**: The command MUST require an explicit output path and MUST preserve existing output unless the user supplies `--overwrite`.
- **FR-008**: The renderer MUST remain offline and MUST NOT invoke an external renderer or send repository content to any service.
- **FR-009**: Native `.drawio` XML MUST contain only generated node, edge, label, and layout data; labels MUST be escaped as text, and output MUST omit scripts, hyperlinks, remote assets, and executable content.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Branch, merge, and cycle fixtures produce valid PNG files containing every node and edge label; repeat renders of unchanged inputs produce byte-identical output.
- **SC-002**: Unsupported grammar, malformed input, missing files, path escapes, and source/graph/image limits return actionable diagnostics and leave the requested output absent or unchanged.
- **SC-003**: Automated checks verify output containment, conflict preservation, explicit overwrite behavior, and absence of network/external-renderer dependencies.
- **SC-004**: A user can find the accepted syntax, limits, command, image behavior, and security model in command help and repository documentation.
- **SC-005**: Tests parse generated `.drawio` files as XML and verify all graph nodes, edges, labels, containment, overwrite behavior, and absence of external-resource or script elements.

## Edge Cases

- Node identifiers are reused with conflicting labels.
- A graph has disconnected components, no root, multiple roots, cycles, or a shared destination.
- Labels contain glyphs unavailable in the bundled font.
- A Markdown document has no flowchart block or more than one flowchart block.
- The same node or edge appears more than once.

## Assumptions

- BrownPants uses its own small declarative flow format and does not parse Mermaid syntax.
- PNG contains rasterized labels and lines only; no embedded scripts, remote assets, HTML, or executable metadata are needed.
- Input must be an explicit Markdown file under the selected repository root. Documentation must make syntax and limits visible so unsupported diagrams fail clearly.
