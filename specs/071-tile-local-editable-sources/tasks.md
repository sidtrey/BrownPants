# Tasks: Tile-Local Editable Sources

**Input**: Design documents from `specs/071-tile-local-editable-sources/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, and `contracts/tile-bundle-manifest.md`

## Phase 1: Editable Tile Views (US1)

**Goal**: Keep one full graph source per bundle and create compact, valid editable XML beside every PNG tile.

**Independent Test**: Generate a multi-tile graph; confirm all tile sidecars contain the correct viewport elements and valid terminals, the complete source contains the full graph, and repeated outputs are deterministic.

- [x] T001 [US1] Add tile-window XML serialization in `src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs`, filtering intersecting nodes/edges and translating geometry while preserving edge endpoint cells and metadata (AC-001, AC-003, FR-002, FR-003).
- [x] T002 [US1] Add one complete bundle `.drawio` path to manifest generation, expected-artifact conflict checks, staging, and commit handling in `src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs` (AC-002, AC-005, FR-004, FR-005).
- [x] T003 [US1] Replace full-template copying with per-tile serialization in `src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs`; preserve atomic overwrite, old-manifest compatibility, safe path validation, and unrelated files (AC-001, AC-003, AC-005, FR-001, FR-005).
- [x] T004 [US1] Add fixture tests in `tests/BrownPants.Tests/LocalFlowchartTests.cs` for isolated/visible nodes, edges entering and leaving tiles, off-page terminals, translated waypoints, complete master contents, and BrownPants import of emitted sidecars (AC-001, AC-002, AC-003, SC-002).
- [x] T005 [US1] Add deterministic repeated-render, conflict/overwrite, stale tile cleanup, manifest validation, and tile XML byte-budget regression cases in `tests/BrownPants.Tests/LocalFlowchartTests.cs` (AC-004, AC-005, SC-003, SC-004).
- [x] T006 [US1] Add `scripts/qa-tile-bundle.ps1` and record its 2,200-node, >1,024-tile end-to-end size, import, manifest, and determinism validation in `specs/071-tile-local-editable-sources/verification.md` (SC-001, SC-002, SC-003).
- [x] T007 [US1] Update tiled output guidance, manifest contract, and size/round-trip limitations in `README.md`, `specs/068-polished-local-flowcharts/contracts/diagram-cli.md`, and `specs/071-tile-local-editable-sources/quickstart.md` (AC-001, AC-002, SC-001).
- [x] T008 [US1] Run `dotnet build BrownPants.sln -c Release`, `./scripts/test.ps1`, acceptance hash/evidence checks, `dotnet list BrownPants.sln package --vulnerable --include-transitive`, and a tiled-renderer source/dependency audit; record observed results in `specs/071-tile-local-editable-sources/verification.md` (SC-001, SC-002, SC-003, SC-004, SC-005, SC-006).
