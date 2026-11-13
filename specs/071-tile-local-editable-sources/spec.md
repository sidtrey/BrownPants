# Feature Specification: Tile-Local Editable Sources

**Feature Branch**: `071-tile-local-editable-sources`
**Created**: 2026-10-16
**Status**: Draft
**Input**: User objective: finish a fully local BrownPants-authored chart engine. BrownPants' 2,200-node stress run exposed repeated full-graph XML sidecars consuming 7.47 GB for 4,308 tiles.

## User Scenarios & Testing

### User Story 1 - Edit a tiled chart without duplicating its full source (Priority: P1)

As a developer reviewing an oversized chart, I need each image tile to have a useful editable diagrams.net file and one complete source file for the whole chart, so I can inspect a tile without storing the entire graph thousands of times.

**Why this priority**: The current tiled output repeats the full graph in every sidecar. A 2,200-node chain generated 4,308 PNGs and 4,308 identical 1.73 MB XML files, using about 7.47 GB in total.

**Independent Test**: Render a generated graph that spans over 1,024 tiles. Validate every manifest path, open each XML document with the bounded local parser, compare the master graph to the source, and verify aggregate sidecar size stays within the stated stress-fixture budget.

**Acceptance Scenarios**:

1. **AC-001** Given a tiled graph, **When** BrownPants writes its bundle, **Then** every PNG has an adjacent editable tile XML containing the nodes in that viewport, the edges whose routes or labels meet it, and any endpoint cells needed to keep those edge references valid.
2. **AC-002** Given the same bundle, **When** BrownPants writes the complete editable source, **Then** it writes the full graph once in the bundle and records its repository-relative path in the tile manifest.
3. **AC-003** Given a tile-local sidecar, **When** it is opened or imported, **Then** its tile coordinates, visible labels, styles, tooltips, edge IDs, source/target IDs, and routed waypoints are preserved; cells outside the viewport may remain off-page when required by a visible edge.
4. **AC-004** Given identical input and options, **When** the bundle is generated twice, **Then** PNGs, tile XML, complete XML, and manifest are byte-identical.
5. **AC-005** Given an existing bundle, **When** overwrite is denied or bundle generation fails, **Then** existing images, sidecars, complete source, manifest, and unrelated files remain unchanged.

## Edge Cases

- An edge crosses a tile while one or both endpoint nodes are outside it.
- A long edge route or edge label touches multiple tiles.
- A tile contains an isolated node and no edge.
- An old manifest lacks the complete-source field.
- The complete-source path or a tile path collides with a user file.
- A failure occurs after some tile XML files have been staged.

## Requirements

### Functional Requirements

- **FR-001**: Tiled output MUST continue to produce a same-stem `.drawio` file for every PNG tile.
- **FR-002**: Each tile sidecar MUST include only nodes that intersect the tile or are required as endpoints of an included edge, and only edges whose routed path or label intersects the tile.
- **FR-003**: Tile sidecars MUST translate all retained geometry by the tile origin and preserve original cell IDs, labels, tooltips, styles, terminals, and waypoints.
- **FR-004**: Tiled output MUST include one complete editable diagrams.net source for the whole graph and expose its path in the manifest.
- **FR-005**: The complete source and all tile sidecars MUST be staged, path-checked, overwrite-protected, and committed transactionally with the PNGs and manifest.
- **FR-006**: Repeated generation with the same source and options MUST produce byte-identical files.
- **FR-007**: The feature MUST use the existing managed .NET/XML implementation and add no charting toolkit, platform drawing API, external process, or network access.

## Success Criteria

- **SC-001**: A deterministic 2,200-node / 2,199-edge chain rendered at 256-pixel tiles and 64-pixel overlap produces more than 1,024 tile pairs, every manifest path resolves, each XML file is well-formed, and the complete bundle's XML output totals no more than 50 MB.
- **SC-002**: The complete source contains all declared nodes and edges exactly once; each tile XML contains every visible node and every intersecting route/label with valid source and target references.
- **SC-003**: Repeated runs produce byte-identical PNG, tile XML, complete XML, and manifest output.
- **SC-004**: Conflict and overwrite tests prove complete-source, tile, manifest, and unrelated-file handling without partial replacement.
- **SC-005**: Release build and all repository tests pass, and package audit reports no vulnerable packages in configured sources.
- **SC-006**: Source and dependency review confirms tiled drawing and XML export have no charting toolkit, platform rendering API, external process, or network dependency.

## Assumptions

- A tile sidecar is an editable viewport excerpt; the bundle's complete source is the authoritative full graph.
- Diagrams.net permits graph cells outside the visible page when they are endpoints of an edge visible in the tile.
- Existing PNG rendering, tile dimensions, overlap, ordering, and no-network behavior remain unchanged.
