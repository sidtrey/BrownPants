# Research: Tile-Local Editable Sources

## Decision

Write one complete `.drawio` file per bundle and one viewport-scoped `.drawio` excerpt per PNG tile. Reuse the existing spatial index to find intersecting elements. Preserve off-page endpoint cells for every included edge so diagrams.net source/target references remain valid.

## Evidence and Rationale

- `LocalFlowchartCommand.WriteTiledBundle` currently serializes one complete XML template and copies it for every tile. The 2,200-node test produced 4,308 XML files at 1,729,641 bytes each (about 7.44 GB of XML).
- `LocalFlowchartSpatialIndex` already returns stable intersecting node and edge indexes, including padding for drawing bounds and edge labels.
- `LocalFlowchartDrawioWriter` serializes every node and edge from a supplied layout, so it can be extended to serialize a filtered, translated tile layout without changing the XML dialect.
- The current draw.io parser preserves cell IDs, terminals, waypoints, and imported geometry, allowing round-trip tests for tile excerpts.
- Compression was evaluated as a lower-effort alternative. Raw-DEFLATE/base64 reduced a representative sidecar from 1,729,641 bytes to 95,840 bytes, but still repeats the entire graph in every tile. A viewport excerpt avoids that multiplication while retaining one canonical full source.

## Alternatives Considered

- Keep complete XML in every tile: unacceptable repeated storage (7.44 GB in the measured run).
- Compress the complete graph in every sidecar: smaller, but still grows with full graph size multiplied by tile count and makes the sidecars harder to inspect.
- Emit only a complete XML source once: efficient, but fails the existing same-stem XML sidecar for every PNG and makes tile inspection harder.
- Use external links, hard links, or platform APIs: not portable and would weaken standalone offline files or the managed .NET boundary.

## Compatibility Reference

The diagrams.net format supports plain XML with `mxfile`/`diagram`/`mxGraphModel` and standard cells. BrownPants remains responsible for serialization and validation; the editor is not a runtime dependency. References: [diagram generation and compressed XML format](https://www.drawio.com/docs/reference/diagram-generation/) and [draw.io file format](https://github.com/jgraph/drawio/wiki/Diagram-File-Format). These sources inform interoperability only.
