# Implementation Plan: Polished Local Flowcharts

**Branch**: `codex/068-polished-local-flowcharts` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Upgrade the deterministic local chart engine with standards-informed process-flow symbols, source-backed call/impact trees, organization charts, and brainstorm maps. Improve graph layout, provide tiled PNG output for large charts, and define a bounded diagrams.net compatibility contract. Keep rendering local and preserve the current `brownpants-flow` authoring format. Standards establish symbol meanings and reading conventions; layout quality is measured separately with fixtures and human review.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: Existing .NET runtime libraries; no new package unless reviewed as necessary.

**Storage**: User-selected local output files; tiled renders are grouped under a dedicated subdirectory named for the requested output stem.

**Testing**: xUnit in `tests/BrownPants.Tests`; end-to-end CLI fixtures.

**Target Platform**: Windows, Linux, macOS where .NET 10 is supported.

**Project Type**: CLI tool.

**Performance Goals**: Keep rendering bounded by documented source, graph, canvas and tile limits; preserve deterministic outputs.

**Constraints**: Offline operation, no remote resources, safe repository-relative paths, explicit overwrite, stable output, compatibility with the existing command. Source call graphs require an explicit semantic-analysis opt-in because MSBuild project evaluation may run imported targets. diagrams.net round-trip support must be bounded to the pinned format documentation and provenance-tracked corpus.

**Scale/Scope**: Accept up to 1,000,000 nodes and edges from sources up to 128 MiB. Keep one-image rendering within a 10%-reserved byte-array budget; use deterministic bounded tiles for larger canvases, within the documented PNG dimension and bundle limits. Put each tiled render in a dedicated subdirectory named for the requested output stem.

Tiled bundle replacement uses the prior manifest to identify stale generated tiles, stages all new files before commit, and preserves unrelated files in the bundle directory.

## Constitution Check

- Evidence First: Pass. Symbol profile and compatibility reports cite source/conformance boundaries; layout outcomes are explicit and fixture-backed.
- Broad, Engine-Neutral Core: Pass. Diagram authoring and rendering do not require an SDD engine or AI host.
- Deterministic and Reviewable Output: Pass. PNGs, tile manifests, and XML use stable ordering and formatting.
- Safe Inspection by Default: Applicable principle adapted. Diagram generation is an explicit user command; it remains local, bounded, and path-safe without project execution or network.
- Specified and Verified Changes: Pass. This feature begins from the frozen spec and will use fixture-based tests and documented acceptance evidence.

No constitution violations identified.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── LocalFlowchartParser.cs
├── LocalFlowchartLayout.cs
├── LocalFlowchartPngRenderer.cs
├── LocalFlowchartDrawioWriter.cs
├── LocalFlowchartDrawioParser.cs
├── LocalFlowchartCommand.cs
├── CallTreeDiagramAnalyzer.cs
└── CallTreeDiagramCommand.cs

tests/BrownPants.Tests/LocalFlowchartTests.cs
  tests/BrownPants.Tests/Fixtures/LocalFlowcharts/
  tests/BrownPants.Tests/Fixtures/DrawioCompatibility/
specs/068-polished-local-flowcharts/
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/diagram-cli.md
```

**Structure Decision**: Extend the existing CLI flowchart pipeline and its fixture suite, and add a caller-graph adapter over the existing semantic inventory and impact graph. Add separate types only where layout, tile manifest, profile or XML import responsibilities need clear boundaries. Do not add a rendering project or dependency until research demonstrates need.

## Design Gate

The main technical risks are XML compatibility scope and readability metrics. Establish an explicit diagrams.net corpus/version before claiming interchange coverage. Use geometry assertions for overlap/routing, image fixtures for visual review, and a documented acceptance scale. Preserve a simpler single-page path when tile output is not requested. Standards conventions should be represented as an attributable symbol profile, not a blanket compliance claim.
