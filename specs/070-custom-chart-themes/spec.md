# Feature Specification: Custom Chart Themes

**Feature Branch**: `070-custom-chart-themes`
**Created**: 2026-10-15
**Status**: Draft
**Input**: User idea queue item 26: let agents provide named or custom chart themes, including a “blue water” palette, with deterministic local PNG and editable XML output.

## User Scenarios & Testing

### User Story 1 - Apply a built-in theme (Priority: P1)

A developer or agent chooses a named BrownPants theme when creating a workflow, code-flow, organization, or brainstorm chart. The chart keeps its current content and layout while using the selected colors in its PNG and editable diagrams.net file.

**Why this priority**: Named themes make chart output immediately customizable without extra files or dependencies.

**Independent Test**: Render the same fixture with the default and each built-in theme; verify stable geometry and graph content, distinct expected pixels, matching XML colors, and byte-for-byte deterministic repeated output.

**Acceptance Scenarios**:

1. **AC-001** — **Given** a valid chart, **When** the user selects `default`, `blue-water`, or `high-contrast`, **Then** BrownPants applies that palette consistently to the PNG and editable XML.
2. **AC-002** — **Given** the same BrownPants-authored chart and theme, **When** it is rendered repeatedly, **Then** PNG and XML bytes are identical and node positions, labels, and connections do not change; omitting the theme option produces the same bytes as explicitly selecting `default`. Imported diagrams without a theme retain their imported XML styles.

### User Story 2 - Supply an agent-created theme (Priority: P1)

An agent creates a small local JSON theme file for a team or request, then passes its repository-relative path to BrownPants. BrownPants validates and applies the theme without loading fonts, executing configuration, or contacting a service.

**Why this priority**: Agents can tailor charts to a team's visual language while keeping all rendering inside the trusted local tool.

**Independent Test**: Use a custom theme fixture to render a PNG and XML sidecar; compare named palette roles in both outputs and verify graph/layout equivalence with default output.

**Acceptance Scenarios**:

1. **AC-003** — **Given** a valid versioned theme file, **When** it is passed to `diagram`, `chart callers`, or `chart flow`, **Then** every supported chart renderer uses its declared colors in the PNG and editable XML, including all tiles.
2. **AC-004** — **Given** an invalid, oversized, unsupported-version, or outside-repository theme path, **When** the command runs, **Then** it returns a specific actionable error and creates no output files.

### User Story 3 - Review themed imported diagrams (Priority: P2)

A developer imports a supported diagrams.net file and requests a BrownPants theme. BrownPants applies the requested palette to supported shapes and edges while preserving labels, connections, geometry, and editable output.

**Why this priority**: Existing team diagrams can be brought into the same local presentation without redrawing them.

**Independent Test**: Import the supported compatibility fixture with and without a theme; verify themed style values, preserved graph structure and waypoints, and unchanged layout geometry.

**Acceptance Scenarios**:

1. **AC-005** — **Given** a supported diagrams.net input and a valid theme, **When** BrownPants renders it, **Then** supported color roles use the selected palette and the round-tripped XML remains editable while geometry and connections are preserved.
2. **AC-006** — **Given** an unsupported imported shape, **When** a theme is requested, **Then** the existing compatibility error remains explicit and no unsupported content is silently dropped.

### Edge Cases

- Missing, malformed, duplicate-property, unknown-property, or invalid-hex-color theme data.
- Invalid UTF-8, empty file, file larger than 16 KiB, or schema version other than 1.
- Theme values with external URLs, markup, font names, or other executable/resource directives.
- Tiled output: every tile and sidecar must use the same palette.
- Theme values with insufficient contrast for readable node and edge labels.
- Existing imported styles and caller/flow charts with no resolved call targets.

## Requirements

### Functional Requirements

- **FR-001**: `diagram`, `chart callers`, and `chart flow` MUST accept one optional named theme or one optional custom theme file; the options MUST be mutually exclusive.
- **FR-002**: BrownPants MUST provide `default`, `blue-water`, and `high-contrast` named themes.
- **FR-003**: A custom theme MUST use a versioned JSON schema with explicit canvas, text, connector, edge-label, shadow, and per-shape fill/stroke color roles.
- **FR-004**: Custom themes MUST be read as bounded repository-relative input without includes, external resources, code execution, or network access.
- **FR-005**: Theme colors MUST use `#RRGGBB`; unknown fields, duplicate JSON keys, missing required roles, and unsupported versions MUST fail with actionable diagnostics.
- **FR-006**: Theme contrast MUST meet WCAG AA 4.5:1 for node and edge-label text against their backgrounds; invalid contrast MUST be reported before output is written.
- **FR-007**: A theme MUST affect PNG, tiled PNGs, editable `.drawio` XML, and PNG sidecars consistently without changing chart meaning, labels, geometry, edge routing, or deterministic ordering.
- **FR-008**: For imported diagrams, applying a theme MUST preserve supported shapes, labels, waypoints, and connectivity while replacing supported presentation colors only.
- **FR-009**: Theme application MUST remain BrownPants-authored managed .NET code and add no graphics toolkit, font dependency, external process, or service dependency.
- **FR-010**: Documentation MUST include the JSON contract, built-in names, a complete custom theme example, and runnable commands for an agent or developer.
- **FR-011**: For BrownPants-authored source charts, omitting the theme option MUST use the `default` palette and produce byte-identical output to explicitly selecting `default`. Imported diagrams without a theme MUST preserve their supported input styles.

### Key Entities

- **Chart theme**: A named, versioned palette of bounded color values assigned to semantic chart roles.
- **Theme role**: A semantic color slot such as canvas, decision fill, process stroke, connector, or edge-label background.
- **Theme selection**: The mutually exclusive named theme or custom repository-relative theme path supplied to a chart command.

## Success Criteria

- **SC-001**: All three named palettes render through every supported chart command, with PNG/XML parity and deterministic repeated bytes.
- **SC-002**: A custom theme renders on regular and tiled outputs, and its role colors match in PNG and editable XML.
- **SC-003**: Every invalid-theme fixture fails before creating any PNG, XML, tile, or manifest output.
- **SC-004**: Tests prove that changing themes leaves node geometry, labels, graph edges, and routing unchanged.
- **SC-005**: Theme parsing and rendering use no theme-specific package, external program, file outside the selected repository, or network access.
- **SC-006**: Built-in and sample custom palettes pass the specified contrast checks, and a palette below 4.5:1 is rejected.
- **SC-007**: BrownPants-authored source chart fixtures produce byte-identical PNG and XML output with the theme option omitted or with `--theme default`; imported input styles remain unchanged when no theme is selected.

## Assumptions

- Theme files are small declarative JSON inputs stored inside the selected repository; BrownPants does not resolve environment variables, includes, or theme inheritance.
- Theme colors are hex RGB only. Fonts, icons, shape selection, spacing, and layout remain controlled by BrownPants.
- The `default` palette remains the default for BrownPants-authored source charts when no theme is selected. Renderer bug fixes may change historical bytes; output determinism and parity with explicit `--theme default` are the compatibility guarantees. Imported diagrams retain input styling unless a palette is explicitly requested.
