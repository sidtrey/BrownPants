# Data Model: Polished Local Flowcharts

## Flow description

- Diagram identifier: stable user-provided name.
- Nodes: stable identifiers, profile symbol kind, label text, and source order.
- Edges: source/target node identifiers, optional label, and source order.
- Validation: all identifiers are unique and referenced nodes exist; resource and text limits are enforced before rendering.

## Symbol profile

- Profile identifier and version.
- Symbol kinds, canonical meaning, visual geometry, orientation, and required text/connector rules.
- References to source conventions and a conformance statement (for example, "informed by ISO 5807", never unsupported blanket compliance).

## Layout

- Direction (top-down or left-to-right), node bounds, edge routes, label bounds, page bounds, and layout-quality diagnostics.
- Invariants: preserve graph identity and connectivity; no unintended node overlap; do not route edges through unrelated node interiors; deterministic tie-breaking.

## Tile manifest

- Diagram identifier and render/profile version.
- Canvas size, tile dimensions, ordered tile identifiers, integer pixel bounds, and shared-edge overlap.
- Every canvas pixel is covered; bounds do not exceed the configured resource limit.

## Compatibility corpus entry

- Source XML revision and sample identifier.
- Supported feature inventory (symbols, styles, connections, labels, pages, geometry).
- Expected round-trip semantic and geometry tolerances, or expected unsupported-feature diagnostic.

## Source call graph

- Root method: repository-relative source path plus exact compiler symbol identity.
- Relationship edge: compiler-resolved caller/target identity, call-site path/line, relationship kind, and hop depth.
- Coverage state: loaded/discovered projects, unresolved relationship counts, sampled/omitted edge counts, and diagnostics.
- QA candidate: test path linked to known call impact with a candidate label; never implies executed coverage.
