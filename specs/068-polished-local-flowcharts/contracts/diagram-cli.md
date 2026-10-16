# Diagram Command Contract

## Existing command

`brownpants diagram <repository-root> <markdown-path> --output <path> [--format png|drawio] [--overwrite]`

- Input remains one Markdown fenced block tagged `brownpants-flow`.
- Node and edge labels are concise visible text; append `details "..."` to a node or labeled edge to retain up to 1,000 characters in an editable XML tooltip without enlarging the PNG. Visible labels are wrapped deterministically and layout reserves their measured bounds.
- Output path remains repository-relative and output is never replaced without `--overwrite`.
- Rendering is local and offline; resource loading and network access are not part of the contract.
- Existing PNG and diagrams.net writer behavior stays available.

## Layout and interchange options

The implemented options are:

- Select `--direction top-down|left-to-right` and the `--profile software` symbol set.
- Export editable diagrams.net XML with `--format drawio`.
- Import diagrams.net XML by setting `--input-format drawio`; the output may be PNG or editable XML. Import never overwrites its source.

The format research baseline is the jgraph/drawio File Format and Security wiki revisions reviewed 2026-10-15 (File Format page edited 2026-10-07). These references describe the format; they are not runtime dependencies. BrownPants uses .NET's XML and compression APIs.

## Implemented tiled PNG output

When single-image RGB raster storage exceeds 90% of .NET's maximum byte-array capacity divided by three bytes per pixel, tiled PNG output is selected automatically with 4,000-pixel tiles and 64-pixel overlap. `--tile-size 256..4000` explicitly selects a tile size and `--overlap <pixels>` overrides the default (must be less than half the tile size). The supplied `--output <name>.png` is the filename stem in tiled mode. All artifacts are grouped under `<output-parent>/<name>/`: row-major `<name>-xXXXXXXX-yYYYYYYY.png` files (X/Y are zero-padded canvas pixel origins), a same-stem `.drawio` sidecar per tile, one `<name>.drawio` complete-source master, and `<name>.tiles.json`. Each tile sidecar is a standalone viewport excerpt: it contains intersecting nodes, edges whose route or label touches the tile, and endpoint cells needed for included edges. All retained geometry, including any off-page endpoints and waypoints, is translated by that tile's canvas origin. The master retains the complete graph once. The manifest root `editableSource` points to the master; each tile's `editableSource` points to its local excerpt. Tile PNGs and XML are written one at a time to a staging directory; the camelCase manifest is streamed to disk, and the staged bundle is committed only after generation succeeds. On `--overwrite`, BrownPants replaces current tiles and removes obsolete tile files named in the previous valid manifest; unrelated files in the output subfolder are preserved. There is no tile-count ceiling; available disk space and filesystem limits bound total output. The manifest records diagram/source identity, canvas dimensions, tile settings, and each tile's order, row, column, paths, origin, dimensions, and shared-edge overlaps. Input is limited to 128 MiB, 1,000,000 nodes, and 1,000,000 edges; logical canvas dimensions stay 10% below PNG's 2^31-1 per-side format maximum. Without automatic/explicit tiling, single-image raster storage stays below the same 90% allocation threshold; scanlines are compressed incrementally. All generated files obey repository-relative path safety and the same `--overwrite` rule.

## Compatibility contract

The implemented import profile is deliberately bounded. It accepts plain and compressed one-page `mxfile` and legacy `mxGraphModel`, one default layer, standard process/decision/terminal/input-output/cloud shapes, a restricted safe style vocabulary, labels/tooltips, integer node geometry, page name and bounds, source/target connections, and integer edge waypoints. The importer carries supported cell IDs, styles, labels, page geometry, node geometry, terminals, and waypoint coordinates through editable XML export. Multiple pages, nested layers, user objects, HTML labels, custom shapes, unsupported attributes/styles, non-integer geometry, external resources, and unmodeled extensions receive a source-qualified diagnostic. PNG rendering uses BrownPants' built-in symbol colors; imported XML styles are preserved in the sidecar.

The fixtures in `tests/BrownPants.Tests/Fixtures/DrawioCompatibility/` are synthetic and independently authored from the pinned format description; provenance and expected behavior are recorded beside them. The corpus covers plain/compressed containers, legacy model, page count, styles, labels, geometry, waypoints, custom shapes, and unsafe DTD input. XML import disables DTDs and external entities and loads no scripts, images, fonts, browser, toolkit, or network resources. Compatibility claims are limited to this tested profile and corpus; BrownPants does not claim 100% diagrams.net compatibility.

The 2026-10-07 upstream page revision describes multiple pages and both plain and compressed page content. Multiple pages currently receive an explicit tested rejection; compressed single-page content is supported. Round-trip tests compare cell identity, labels, supported shape semantics, default-layer relationships, terminals, page bounds, node geometry, styles, and waypoints. Integer geometry is preserved exactly in the covered corpus. Import and render stay fully local; no diagrams.net runtime, browser, toolkit, or network resource is loaded.

## Source-backed call diagrams

`brownpants chart callers <repository> --source <relative.cs-or-vb-path> --method <exact-fully-qualified-symbol> --semantic --output <relative.png-or-drawio> [--format png|drawio] [--direction top-down|left-to-right] [--depth 1..100] [--limit 1..10000] [--tile-size 256..4000] [--overlap pixels] [--overwrite]`

- The command requires `--semantic` to resolve C#/Visual Basic calls through the semantic project graph. This explicit opt-in may evaluate imported MSBuild targets; host instructions must honor repository and user policy.
- Method selection must exactly match a compiler symbol declared in `--source`; a missing or ambiguous declaration is rejected. All displayed call edges carry source path, line, and hop depth.
- The chart exposes project coverage, unresolved relationships, and omitted edges. It follows compiler-resolved C#/VB relationships only; native, reflection, dynamic, and unresolved relationships are not inferred.
- Paths recognized as tests are visually labeled as candidates. They do not prove runtime test coverage.
- Call-site nodes show a compact repository-relative path and line; their editable XML tooltips retain the full caller symbol, full path, relationship kind, target, and hop depth. Unicode text is preserved in diagrams.net XML; the built-in PNG font replaces characters outside printable ASCII with `?`.
- Host instructions map requests such as “show me everything that calls this function” to explicit arguments; BrownPants itself does not call an LLM.
