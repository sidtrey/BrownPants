# Data Model: Local Flowchart

The graph exists only in memory during parsing and rendering.

## FlowchartDocument

| Field | Meaning | Validation |
|---|---|---|
| `Id` | Diagram identifier | ASCII identifier, 1–40 characters. |
| `Nodes` | Nodes in declaration order | 1–200 unique IDs; each has one of four shapes and a supported label. |
| `Edges` | Directed relationships in declaration order | 0–500 unique source/target pairs; both IDs must exist. |
| `SourcePath` | Repository-relative Markdown path | Must stay under root and contain no symbolic link/reparse point. |
| `Line` | One-based source line for declarations | Used in parser and validation diagnostics. |

## FlowNode

`Id`, `Shape` (`Start`, `Process`, `Decision`, `End`), `Label`, `SourceLine`, and stable declaration `Order`.

## FlowEdge

`FromId`, `ToId`, optional `Label`, `SourceLine`, and stable declaration `Order`. Shared targets and back edges remain distinct relationships.

## RenderedArtifact

`Format` (`Png` or `Drawio`), relative `OutputPath`, and encoded bytes. PNG carries bounded pixel width/height; draw.io XML carries one page with vertex and edge cells. Output is staged before commit to the destination and existing files are preserved unless overwrite is explicit.

## Limits

Source Markdown: 1 MiB. Graph: 200 nodes, 500 edges. PNG canvas: at most 4096 by 4096 pixels and 16 million pixels total. Labels use supported printable ASCII only; unsupported glyphs fail with their source line.
