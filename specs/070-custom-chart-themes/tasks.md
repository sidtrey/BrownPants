# Tasks: Custom Chart Themes

**Input**: Design documents from `specs/070-custom-chart-themes/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/chart-theme.schema.json`
**Tests**: Required by the acceptance scenarios; add deterministic fixture and CLI tests.

## Phase 1: Setup

- [x] T001 [P] Add a byte-parity regression test proving omitted theme selection matches explicit `--theme default` for PNG and XML in `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-002, SC-007).

## Phase 2: Foundational

- [x] T002 Define immutable RGB and `ChartTheme` models, default palette constants, and shape-role mapping in `src/BrownPants.Cli/Analysis/ChartTheme.cs` (AC-001, AC-003).
- [x] T003 Add the strict schema-v1 parser, duplicate/unknown property checks, bounded UTF-8 reading, and 4.5:1 contrast validation in `src/BrownPants.Cli/Analysis/ChartThemeParser.cs`; test valid, invalid, and built-in palettes in `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-001, AC-004, SC-006).

## Phase 3: User Story 1 - Apply a built-in theme

**Goal**: Developers and agents can select named palettes and receive deterministic themed charts.

**Independent Test**: Render a fixture with all built-ins through `diagram`, `chart callers`, and `chart flow`; compare PNG/XML role colors, graph structure, geometry, and repeat hashes.

- [x] T004 Parse mutually exclusive `--theme` and `--theme-file` options for `diagram` in `src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs` and add CLI option tests in `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-001, AC-004).
- [x] T005 Pass named palettes through the PNG renderer and diagrams.net writer in `src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs` and `src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs`; assert palette parity and that unthemed golden bytes remain unchanged in `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-001, AC-002, SC-001, SC-004, SC-007).
- [x] T006 Add `default`, `blue-water`, and `high-contrast` palette fixtures and deterministic rendering assertions in `tests/BrownPants.Tests/Fixtures/ChartThemes/blue-water.json` and `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-001, AC-002, SC-001, SC-006).

## Phase 4: User Story 2 - Supply an agent-created theme

**Goal**: An agent can pass a reviewed local JSON palette to every source-chart command.

**Independent Test**: Custom files produce declared colors in PNG and XML across normal/tiled, call-tree, and method-flow charts; rejected files produce actionable errors and no output.

- [x] T007 Add bounded repository-relative `--theme-file` handling to `src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs`, including reparse-point checks before output staging; cover path escape, malformed JSON, size limit, invalid colors, and no-output failures in `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-003, AC-004, SC-002, SC-003, SC-005).
- [x] T008 Thread the same validated theme through caller and method-flow commands in `src/BrownPants.Cli/Analysis/CallTreeDiagramCommand.cs` and `src/BrownPants.Cli/Analysis/MethodControlFlowDiagramCommand.cs`; verify named and custom themes in `tests/BrownPants.Tests/MethodControlFlowDiagramTests.cs` (AC-003, SC-002, SC-004).
- [x] T009 Verify tiled rendering applies one palette to every PNG and editable XML tile and preserves manifest geometry in `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-003, SC-002, SC-004).

## Phase 5: User Story 3 - Review themed imported diagrams

**Goal**: Supported diagrams.net imports can be restyled without changing their graph or geometry.

**Independent Test**: Import the supported fixture with a custom theme and compare shape/edge values, labels, connections, waypoints, and node bounds.

- [x] T010 Apply theme colors to supported imported shape and edge style keys in `src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs`; test round-trip graph, labels, waypoints, and non-color styles in `tests/BrownPants.Tests/ChartThemeTests.cs` (AC-005, AC-006, SC-004).

## Phase 6: Polish and cross-cutting concerns

- [x] T011 Document built-in palettes, JSON roles, limits, and runnable `diagram`, caller, and method-flow examples in `docs/chart-themes.md` and `README.md` (AC-003, SC-001, SC-002).
- [x] T012 Record verification for all criteria, run the Release build/full tests/package audit, and validate acceptance hash/evidence in `specs/070-custom-chart-themes/verification.md` and `specs/070-custom-chart-themes/acceptance-evidence.json` (AC-001, AC-002, AC-003, AC-004, AC-005, AC-006, SC-001, SC-002, SC-003, SC-004, SC-005, SC-006, SC-007).

## Dependencies

- T001 establishes current output evidence before renderer edits.
- T002 and T003 are foundational; T004-T010 depend on them.
- US1 (T004-T006) establishes named-theme rendering; US2 (T007-T009) adds custom file input and all source-chart paths; US3 (T010) adds imported diagram restyling.
- T011 documentation follows command and contract behavior; T012 is final verification.

## Parallel Opportunities

- T001 can be completed before theme model implementation.
- After T003, T011's draft contract prose can proceed alongside implementation, then be reconciled with final CLI help.
- Imported-diagram tests in T010 can be authored alongside US2 tests if they use separate methods/fixtures.

## Implementation Strategy

1. Establish output golden bytes and schema tests before changing renderers.
2. Deliver named themes end-to-end as the MVP, then custom theme files for all commands.
3. Add themed diagrams.net import, update documentation, and verify all ACs before release.
