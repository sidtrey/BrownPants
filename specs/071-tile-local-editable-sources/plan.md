# Implementation Plan: Tile-Local Editable Sources

**Branch**: `071-tile-local-editable-sources` | **Date**: 2026-10-16 | **Spec**: [spec.md](spec.md)

## Summary

Keep one canonical full diagrams.net source in each tiled bundle and create a compact editable viewport excerpt beside each tile. Filter nodes and edges through the existing spatial index, translate retained coordinates to the tile origin, and preserve off-page endpoint cells when visible edges need them. Extend bundle staging, manifest validation, and overwrite handling for the new canonical file.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: .NET XML, JSON, compression, and filesystem APIs already in use; no charting package
**Storage**: Repository-local PNG, `.drawio`, and JSON files
**Testing**: xUnit; fixture-backed CLI integration tests; `scripts/test.ps1`
**Target Platform**: Windows, Linux, macOS
**Project Type**: CLI
**Performance Goals**: XML output for the 2,200-node stress fixture stays at or below 50 MB for 4,308 tiles
**Constraints**: Preserve deterministic XML/PNG/manifest output, tile overlap and ordering, repository path safety, no external renderer/network, and transactional overwrite behavior.
**Scale/Scope**: Tiled output from graphs up to current input limits; edge and node inclusion must be proportional to each tile's spatial contents, with one full graph XML per bundle.

## Constitution Check

- Evidence First: tile-local omissions are defined by geometric intersection; the canonical source preserves the full graph.
- Broad, Engine-Neutral Core: output stays ordinary diagrams.net XML and does not depend on an SDD engine.
- Deterministic and Reviewable Output: stable cell ordering, translated coordinates, and manifest field are tested byte-for-byte.
- Safe Inspection by Default: no network or source execution; all paths remain inside the selected repository.
- Specified and Verified Changes: acceptance criteria, task mappings, regression tests, and output limits are explicit.

## Design

### Bundle contract

- Keep existing tile image and same-stem `.drawio` paths.
- Add `<bundle>/<name>.drawio` as the sole complete graph source and add `editableSource` at the manifest root.
- Each tile sidecar uses the tile page bounds and local coordinates. Include intersecting visible nodes, isolated visible nodes, and all endpoints for edges whose route or placed label intersects that viewport. Preserve the full route for those edges; endpoint cells may lie off-page.
- Add the canonical file to conflict checks, staging, atomic commit, previous-manifest validation, and stale bundle handling.

### Project Structure

```text
src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs
src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs
src/BrownPants.Cli/Analysis/LocalFlowchartSpatialIndex.cs
tests/BrownPants.Tests/LocalFlowchartTests.cs
specs/071-tile-local-editable-sources/
```

## Complexity Tracking

No constitution violations or new dependencies are proposed.
