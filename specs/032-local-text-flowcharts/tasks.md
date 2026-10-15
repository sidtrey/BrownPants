# Tasks: Local Text Flowcharts

**Input**: [spec.md](spec.md), [plan.md](plan.md), and the design artifacts in this feature directory.
**User Story**: US1 is the single P1 story. Tests are required by the spec and constitution.

## Phase 1: User Story 1 — Render a flowchart locally as a PNG (P1)

**Goal**: Convert a local BrownPants flow block into a safe PNG or optional diagrams.net XML file.

**Independent Test**: Fixture tests verify valid graph formats, PNG and XML integrity, deterministic output, source/output path safety, overwrite behavior, and actionable failures.

- [X] T001 [US1] Add parser, renderer, CLI, path-safety, overwrite, and no-partial-output contract tests in `tests/BrownPants.Tests/LocalFlowchartTests.cs`; the focused suite passes.
- [X] T002 [US1] Implement the `brownpants-flow` Markdown block parser, graph model, diagnostics, and graph/resource validation in `src/BrownPants.Cli/Analysis/LocalFlowchartParser.cs`.
- [X] T003 [P] [US1] Implement stable bounded graph layout in `src/BrownPants.Cli/Analysis/LocalFlowchartLayout.cs`, then rasterize shapes, edges, and labels with the bundled bitmap font and BCL PNG encoder in `src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs`.
- [X] T004 [P] [US1] Implement deterministic native uncompressed diagrams.net XML serialization with escaped plain-text labels and no external content in `src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs`.
- [X] T005 [US1] Add explicit input/output path validation, format selection, staged writes, conflict/overwrite handling, and command routing/help in `src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs` and `src/BrownPants.Cli/Program.cs`.
- [X] T006 [US1] Document the BrownPants DSL, limits, PNG and draw.io workflows, and no-network security behavior in `README.md` and `integrations/README.md`.
- [X] T007 [US1] Run focused and full Release tests, build, acceptance hash/evidence checks, inspect generated fixture files, and run the packed-tool smoke steps defined in `.github/workflows/ci.yml`; record observed results in `specs/032-local-text-flowcharts/verification.md`.

## Dependencies & Execution Order

- T001 precedes parser and renderer implementation. T002 precedes T003, T004, and T005.
- T003 and T004 touch separate files and may proceed independently after T002.
- T005 depends on both output formats. T006 documents the completed command. T007 is the final validation gate.

## Implementation Strategy

Deliver one independently usable story. First prove the CLI contract with failing tests, then add the parser, PNG and `.drawio` outputs, and safe command integration. Finish with docs and full release-quality verification.
