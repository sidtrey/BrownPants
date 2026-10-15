# Implementation Plan: Local Text Flowcharts

**Branch**: `032-local-text-flowcharts` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Add a BrownPants-owned flow DSL and pure .NET renderer that creates PNG files by default or native `.drawio` XML on request. The implementation reads only an explicitly selected Markdown file under the repository root, performs no network access, and writes only the explicit in-repository output after complete validation.

## Technical Context

**Language/Version**: C# / .NET 10.  
**Primary Dependencies**: .NET base class library only; no Mermaid, browser, graphics, or image packages.  
**Storage**: No persistent state; explicit diagram output files only.  
**Testing**: xUnit fixtures in `tests/BrownPants.Tests/LocalFlowchartTests.cs`.  
**Target Platform**: .NET 10 supported platforms.  
**Project Type**: CLI.  
**Performance Goals**: Bound input to 1 MiB, graph to 200 nodes/500 edges, canvas to 4096 pixels per dimension and 16 million pixels total.  
**Constraints**: Deterministic, offline and safe. Input/output paths must be contained and reparse-point free. PNG uses a bundled ASCII bitmap font and a managed PNG encoder; unsupported glyphs fail visibly.

## Constitution Check

- **Evidence First**: This is a deterministic rendering tool; it makes no source-analysis claims.
- **Broad, Engine-Neutral Core**: BrownPants owns the small flow format; it is not tied to Spec Kit or Mermaid.
- **Deterministic and Reviewable Output**: Stable source order, layout, PNG encoding, and XML serialization are required.
- **Safe Inspection by Default**: The command is explicit, local-only, path-contained, size-bounded, and writes only a user-selected target. Existing targets require `--overwrite`.
- **Specified and Verified Changes**: Parser, rendering, path, output, and help behavior receive fixture tests; command changes receive a packed-tool smoke check.

All gates pass. No constitution exception is required.

## Project Structure

```text
specs/032-local-text-flowcharts/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/local-flowchart.md

src/BrownPants.Cli/Analysis/LocalFlowchartParser.cs
src/BrownPants.Cli/Analysis/LocalFlowchartLayout.cs
src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs
src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs
src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs
src/BrownPants.Cli/Program.cs
tests/BrownPants.Tests/LocalFlowchartTests.cs
integrations/README.md
README.md
```

**Structure Decision**: Keep parsing, graph layout, PNG raster/encoding, and native XML writing in focused analysis files. Program owns safe path validation, command options, and atomic output handling. Tests call the CLI entry point with temporary repositories.

## Design

`LocalFlowchartParser` extracts exactly one `brownpants-flow` fenced block and parses `flowchart`, `node`, and `edge` statements. It validates declarations, references, ASCII labels, duplicate IDs/edges, and resource limits before returning an immutable graph. `LocalFlowchartLayoutEngine` assigns stable top-down ranks and coordinates, preserving declaration order; back edges and shared targets use routed reference lanes and are never discarded. `LocalFlowchartPngRenderer` draws shapes, arrows, and labels into a bounded RGB buffer with a bundled public-domain bitmap font, then encodes PNG chunks with the BCL zlib stream and deterministic metadata. `LocalFlowchartDrawioWriter` serializes native uncompressed diagrams.net XML using `XmlWriter`, escaped text labels, stable IDs/attributes, and no links or remote assets. `LocalFlowchartCommand` validates paths, selects the output format, stages output, and handles explicit overwrite; `Program` routes and documents the command.

The CLI accepts `diagram <repo> <relative-markdown-path> --output <relative-path> [--format png|drawio] [--overwrite]`. The default is PNG. Format and output extension must agree. Complete output is staged beside the destination and atomically moved into place; validation or rendering failures leave the selected output unchanged. Both source and destination reject path escapes and reparse points.

## Verification

Test valid branches, merges, cycles, multiple roots, output determinism, PNG structure/dimensions, `.drawio` XML structure/content, and command help. Test malformed and multiple blocks, missing/undeclared/conflicting nodes, duplicate edges, unsupported glyphs, source/output escapes, reparse points, source/node/edge/canvas limits, output conflicts, explicit overwrite, and no output on failure. Run focused and complete Release tests, build, acceptance hash/ledger checks, and a packed CLI smoke test.

## Complexity Tracking

No constitution violations. A bundled bitmap font and BCL PNG encoder avoid native or hosted rendering dependencies, keeping all rendering local and portable.
