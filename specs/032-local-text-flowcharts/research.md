# Research: Local Text Flowcharts

## Decision: Use a BrownPants-owned flow DSL

The source is one fenced `brownpants-flow` block with a `flowchart <id>` header, `node <id> <shape> "<label>"` declarations, and `edge <from> -> <to> [label "<text>"]` relationships. It is not Mermaid syntax.

**Rationale**: The maintainer says security-sensitive teams may prohibit Mermaid tooling because of its AI/service association. A small purpose-built declarative input is easy to review and has no AI, browser, package, or service dependency.

**Alternatives considered**: Hosted Mermaid/diagram services violate offline handling goals. Parsing Mermaid while claiming independence would preserve the prohibited format association. General Mermaid compatibility would expand scope and external compatibility surface substantially.

## Decision: Render PNG with portable managed code

Draw shapes and text into a fixed RGB buffer with a bundled bitmap font and write PNG chunks with .NET compression and checksum code. Do not use System.Drawing, native graphics libraries, browser automation, or external processes.

**Rationale**: System.Drawing support is platform-dependent, while the app targets Windows, Linux, and macOS. A small custom rasterizer is auditable and has no native dependencies.

**Alternatives considered**: Native graphics/image packages add dependencies, licenses, or native runtime assets. SVG does not meet the requested PNG output and can contain active/external references if handled loosely.

## Decision: Provide native diagrams.net XML as an optional output

Emit a stable, uncompressed `.drawio` document using the documented `mxfile` / `mxGraphModel` shape and edge cells. Escape every label as plain text and omit scripts, links, images, and remote resources.

**Rationale**: diagrams.net documents `.drawio` XML as its native import format, enabling optional offline editing by teams whose security policy permits the desktop application. PNG remains the portable default.

**Sources**: [draw.io import formats](https://www.drawio.com/docs/manual/import/import-formats/), [draw.io native formats](https://www.drawio.com/docs/manual/editor/save-file-formats/), [draw.io XML structure](https://www.drawio.com/docs/reference/diagram-generation/).

## Decision: Validate both paths and only replace output explicitly

Use repository-relative source/output paths, reject outside paths and reparse points, cap bytes/nodes/edges/pixels, and require `--overwrite` for an existing target. Parse and render fully before writing.

**Rationale**: Diagram input is repository content and output is a user-selected artifact. These rules prevent configuration from expanding reads and prevent accidental overwrite.
