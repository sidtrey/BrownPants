# diagrams.net Compatibility Corpus

All XML in this directory is synthetic, independently authored for BrownPants from the format description in the [jgraph/drawio File Format wiki](https://github.com/jgraph/drawio/wiki/File-Format), page revision edited 2026-10-07 and reviewed 2026-10-15. No user diagrams, upstream sample diagrams, icons, or standards artwork are included. The pinned format and security references are recorded in `specs/068-polished-local-flowcharts/research.md`.

| Fixture | Provenance | Expected result |
|---|---|---|
| `plain-basic.drawio` | Synthetic one-page `<mxfile>` with a process vertex and directed edge, following the documented minimal structure. | Supported baseline: IDs, labels, shapes, terminals, and integer geometry. |
| `waypoints-page.drawio` | Synthetic one-page graph with a named page, custom page bounds, supported cell styles, and two orthogonal edge waypoints. | Preserve page identity/bounds, supported style, vertex geometry, labels, and waypoint coordinates. |
| `legacy-model.drawio` | Synthetic bare `<mxGraphModel>` with root/layer and one vertex. | Supported legacy container variant. |
| `multiple-pages.drawio` | Synthetic two-page `<mxfile>` with unique page/cell IDs. | Preserve both pages or reject explicitly; never silently select one. |
| `compressed-page.drawio` | Synthetic XML page encoded with URI-encoded raw DEFLATE and Base64 as described in the pinned compression section. | Preserve the supported graph or return a specific decompression/unsupported diagnostic. |
| `unsupported-custom-shape.drawio` | Synthetic vertex using a custom `shape=` style, which requires a stencil/runtime not bundled by BrownPants. | Explicit unsupported-shape diagnostic; no external resource load. |
| `unsafe-doctype.drawio` | Synthetic DTD/entity payload. | Reject before resolving or expanding entities. |

The corpus separates format recognition from implementation status: until import and round-trip tests pass, expected behavior is a contract, not a claim that the current CLI handles the sample.
