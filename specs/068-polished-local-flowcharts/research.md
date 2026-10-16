# Research: Polished Local Flowcharts

## Standards and conventions

- **Decision**: Use ISO 5807:1985 as the primary software/system flowchart vocabulary reference. Its published abstract covers symbols and use conventions for data, program, and system flowcharts; ISO says the edition was confirmed in 2019 and remains current. Source: [ISO 5807:1985](https://www.iso.org/standard/11955.html).
- **Decision**: Consult NIST-hosted FIPS PUB 24 (ANSI X3.5-1970) as a publicly accessible historical standard for concrete symbol geometry, orientation, and flow direction. It specifies normal flow left-to-right and top-to-bottom, arrowheads for reverse direction, and maintaining symbol proportions. Source: [FIPS PUB 24](https://nvlpubs.nist.gov/nistpubs/Legacy/FIPS/fipspub24.pdf).
- **Decision**: Treat IBM's Flowcharting Techniques manual (C20-8152 / later GC20-8152) as historical practice and communication guidance, not a current standard or compliance authority. Available digitizations are hosted by third parties; research can inform independent design but must not reproduce protected artwork or templates. A searchable historical copy describes IBM's standardized symbols/templates as intended to improve communication. Source: [archived IBM manual preview](https://www.eserviceinfo.com/preview_html.php?fileid=79135&previewid=49623).
- **Decision**: Respect PNG's 2^31−1 pixel-side field maximum, as specified by the [W3C PNG Third Edition](https://www.w3.org/TR/png/), while reserving 10% headroom. The in-memory single-image renderer uses the tighter byte-array capacity limit; charts above it use bounded tiles.
- **Alternatives considered**: Add ISA instrumentation diagrams to the initial profile. Rejected for this iteration because they describe industrial process/instrumentation diagrams rather than software control flow; reserve them for a future named profile if users request it.

## Adoption guidance

1. Define the initial profile around start/end, process, decision, input/output, predefined process, connector, and parallel/concurrency semantics only where the DSL can represent them unambiguously.
2. Keep labels upright and readable regardless of flow direction; use explicit arrowheads for reverse/back edges and connectors for page/tile breaks.
3. Distinguish shape meaning by geometry and text, not color alone. Preserve conventional proportions where the selected profile specifies them.
4. Attribute each rule and label the profile as "informed by" a standard unless all normative requirements and licensing conditions are verified.
5. Use graph-layout goals (crossing minimization, spacing, hierarchy) as product design criteria rather than attributing them to ISO/IBM.

## Existing implementation evidence

- `LocalFlowchartParser` accepts one fenced `brownpants-flow` block, directed edges, printable ASCII labels, up to 1,000,000 nodes/1,000,000 edges, and source-aware validation; source input is limited to 128 MiB.
- `LocalFlowchartLayout` assigns ranks with one-time outgoing-edge indexing and uses rank-pair edge indexes for crossing reduction. It places same-rank nodes in ordered rows and permits canvases up to 90% of PNG's 2^31-1 per-side field limit. Single-image raster storage is capped at 90% of .NET's maximum byte-array length divided by three RGB bytes per pixel, and PNG scanlines are encoded incrementally. Tiled mode uses the same full-canvas layout with 256–4000 pixel regions, a default 64-pixel overlap, automatic fallback when one-image capacity is exceeded, streamed output generation, and no tile-count ceiling.
- `LocalFlowchartPngRenderer` draws each requested tile from the same global layout coordinates and clips drawing to the tile bounds. A shared 1,024-pixel spatial grid indexes nodes and edge bounds so each tile only rasterizes intersecting candidates; very large shapes/edges use a bounded fallback list. The manifest records stable row-major order, image and XML paths, pixel bounds, and the overlap with prior row/column neighbors.
- `LocalFlowchartPngRenderer` draws deterministic raster PNGs with BrownPants-authored layout, rasterization, PNG encoding, and basic orthogonal routing. Its only embedded third-party visual data is an ASCII bitmap glyph table derived from [`font8x8_basic.h`](https://github.com/dhepper/font8x8/blob/master/font8x8_basic.h); that upstream file identifies the glyph data as public domain and attributes its basis to IBM's public-domain VGA font. No font package, native drawing API, external process, or remote resource is loaded at runtime.
- `LocalFlowchartDrawioWriter` emits a small subset of `mxfile`/`mxGraphModel` XML. It does not import general diagrams.net files, so existing tests prove writer determinism and simple structure, not full round-trip compatibility.
- The existing test fixture covers a branch, labels, an edge returning to an earlier node, disconnected components, rootless cycles, invalid input, path safety, and deterministic output. Add geometry-focused and visual fixtures before expanding claims.

## Dependency decision

Keep the rendering pipeline free of chart/image/font/XML packages. Evaluate any proposed runtime dependency using maintenance, license, vulnerability, and offline behavior evidence before adoption. The existing .NET primitives and BrownPants-authored code are adequate for layout, rasterization, PNG encoding, and XML writing. The embedded glyph table is public-domain visual data, not an executable toolkit or runtime dependency.

## Concrete conventions to consider

- Give symbols distinct meanings through both geometry and labels: process, decision, input/output, predefined process, terminal, connector, annotation, and parallel-mode where the DSL can represent them.
- Label each decision exit with its predicate/result (for example, `true`/`false`, `case`, or `default`). Avoid branches whose meaning is only implied by their location.
- Keep symbol orientations and distinctive proportions consistent. Use one primary reading direction (top-to-bottom, with left-to-right supported) and explicit arrowheads on reverse-direction edges.
- Avoid crossings where possible. If a crossing remains, make junction versus non-junction semantics unambiguous. When flow continues across a tile/page boundary, use paired labeled connectors rather than clipping or terminating the edge silently.
- Keep labels upright and readable; these rules inform semantics and clarity, while spacing, typography, and visual hierarchy remain BrownPants quality goals.

## Copyright and attribution boundaries

- ISO states its publications are copyrighted and reuse/reproduction requires permission: [ISO copyright](https://www.iso.org/copyright.html). Do not include ISO text, figures, scans, or traced artwork in the repository or product.
- FIPS PUB 24 is an ANSI X3.5-1970 publication hosted by NIST. Its text identifies ANSI copyright; NIST hosting is not a blanket license to reproduce the document. Consult the actual publication's reuse statements before using any exact shape/artwork. Prefer an independently authored profile and cite the publication for conceptual conventions only.
- IBM C20-8152 is available here as an archival scan, not an IBM-hosted current standard. Avoid copying its wording, template, branded assets, or illustrations. Attribute historical context carefully and verify edition-specific claims.
- Public-domain font provenance already present in the current renderer is separate; do not assume it licenses diagrams or standards material.

## diagrams.net interchange findings

- **Pinned baseline**: The [jgraph/drawio File Format wiki](https://github.com/jgraph/drawio/wiki/File-Format), page revision edited **2026-10-07** (the revision linked by GitHub's page history on 2026-10-15). The [Security wiki](https://github.com/jgraph/drawio/wiki/Security) is pinned to the same review date. These are documentation references, not runtime dependencies; implementation remains on .NET XML, compression, and URI APIs.
- **Decision**: Treat diagrams.net as an evolving XML family, not a single immutable schema version. The pinned file-format page describes `<mxfile>` with multiple `<diagram>` pages, older bare `<mxGraphModel>` files, plain and compressed page content, style strings, and embedded diagram data. Corpus examples are independently authored from the documented structure; none are copied diagrams or artwork. Record provenance and expected support per fixture.
- **Decision**: Include representative corpus cases for plain and compressed `<mxfile>`, legacy `<mxGraphModel>`, multiple pages, layer/cell structure, vertices/edges, style strings, geometry, labels, and unsupported custom objects. Avoid calling all diagrams.net files supported; state which constructs round-trip.
- **Security**: Upstream says files do not require DTDs/external entities and advises non-browser parsers to disable them. Import must use bounded XML parsing, prohibit DTD/entity resolution, reject remote resources/scripts, and preserve unsupported constructs only if that can be done safely and transparently. Use ordinal ID maps for user-supplied cell IDs.
- **Alternatives considered**: Use a third-party XSD as the compatibility authority. Rejected because a community schema may be useful for validation but does not define every dynamic style or editor behavior. The canonical upstream file-format documentation plus real fixtures and diagrams.net open/save round trips are the compatibility reference.
