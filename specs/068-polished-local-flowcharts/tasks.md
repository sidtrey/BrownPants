# Tasks: Polished Local Flowcharts

**Input**: Design documents from `/specs/068-polished-local-flowcharts/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Organization**: Tasks map directly to the four independently reviewable user stories.

## Phase 1: Setup

- [x] T001 Record the initial symbol profile and diagrams.net XML scope decisions in `specs/068-polished-local-flowcharts/verification.md`, including chosen standard-informed rules and explicit non-conformance wording.
- [x] T002 [P] Add the dense branching, long-label, cycle, and mixed-path `.brownpants-flow` fixtures under `tests/BrownPants.Tests/Fixtures/LocalFlowcharts/`.

## Phase 2: Foundational

- [x] T003 Add geometry-level test helpers for node overlap, label clipping, connector/node intersections, and edge crossing counts in `tests/BrownPants.Tests/LocalFlowchartTests.cs`.
- [x] T004 Define stable render options, symbol-profile identity, and layout diagnostics in `src/BrownPants.Cli/Analysis/LocalFlowchartParser.cs` and `src/BrownPants.Cli/Analysis/LocalFlowchartLayout.cs` without changing default CLI behavior.
- [x] T005 Document standards sourcing, licensing boundaries, and the selected diagram interchange contract in `specs/068-polished-local-flowcharts/research.md` and `specs/068-polished-local-flowcharts/contracts/diagram-cli.md`.

## Phase 3: User Story 1 - Read a polished flowchart (Priority: P1) 🎯 MVP

**Goal**: Produce professional, legible, standards-informed diagrams with understandable graph flow.

**Independent Test**: Run the flowchart fixture suite and inspect the showcase image at its documented display scale; verify AC-001 and AC-002 plus SC-001, SC-002, SC-004, SC-005, and SC-007.

- [x] T006 [P] [US1] Add parser and contract tests for named symbol profile, branch labels, reverse-flow arrowheads, and unsupported symbol diagnostics in `tests/BrownPants.Tests/LocalFlowchartTests.cs`.
- [x] T007 [P] [US1] Add geometry assertions for the dense branch, long labels, cycles, disconnected components, self-loops, parallel edges, and unequal path lengths in `tests/BrownPants.Tests/LocalFlowchartTests.cs`.
- [x] T008 [US1] Implement documented top-down and left-to-right ranking with stable ordering and collision-free node bounds in `src/BrownPants.Cli/Analysis/LocalFlowchartLayout.cs`.
- [x] T009 [US1] Improve orthogonal edge routing to avoid node interiors, reduce crossings, and diagnose remaining crossings without altering graph connectivity in `src/BrownPants.Cli/Analysis/LocalFlowchartLayout.cs`.
- [x] T010 [US1] Render the selected standards-informed symbols, consistent arrows, wrapped text, and legible edge labels in `src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs`.
- [x] T011 [US1] Expose layout direction and symbol profile options through `src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs` and document defaults in `README.md`.
- [ ] T012 [US1] Generate and visually inspect the showcase chart, record display scale, and gather five stakeholder scores against the simple/elegant/usable/useful rubric for AC-002/AC-022 and SC-007/SC-017.

## Phase 4: User Story 2 - Generate source-backed call and QA diagrams (Priority: P2)

**Goal**: Let an agent turn an exact source method request into a resolved caller tree or QA-oriented impact diagram.

**Independent Test**: On the C# fixture, request a caller diagram and verify method identities, direct/transitive call-site locations, partial-coverage warnings, and candidate-only QA links. Verify AC-009 through AC-012 and SC-008/SC-009.

- [x] T013 [P] [US2] Add caller-tree fixture tests for exact target selection, direct/transitive callers, call-site line numbers, and hop depths in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`.
- [x] T014 [P] [US2] Add partial-coverage, unresolved-call, sample-omission, and test-candidate classification tests in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`.
- [x] T015 [US2] Build a source-backed graph model from compiler-resolved declarations and invocation edges, preserving coverage and omission evidence, in `src/BrownPants.Cli/Analysis/CallTreeDiagramAnalyzer.cs`.
- [x] T016 [US2] Add the structured `brownpants chart callers` command with repository-relative `--source`, exact `--method`, direction/depth/sample bounds, required `--semantic` opt-in, and safe output handling in `src/BrownPants.Cli/Analysis/CallTreeDiagramCommand.cs` and `src/BrownPants.Cli/Program.cs`.
- [x] T017 [US2] Render root methods, resolved caller edges, source path/line labels, hop depth, and candidate test-path nodes through the shared layout and editable sidecar writers.
- [x] T018 [US2] Add agent-facing guidance that maps natural-language caller/test-impact requests to the explicit CLI and explains semantic-coverage limitations; explain that `--semantic` may evaluate imported MSBuild targets.
## Phase 5: User Story 3 - Share large diagrams safely (Priority: P3)

**Goal**: Render oversized diagrams as ordered, deterministic local PNG tiles.

**Independent Test**: Generate multi-tile output with network disabled and verify complete bounds, overlap, stable manifest, offline behavior, and AC-003/AC-004 plus SC-003.

- [x] T019 [P] [US3] Add tile-boundary, overlap, manifest, and deterministic-output tests in `tests/BrownPants.Tests/LocalFlowchartTests.cs`; local rendering has no network dependency.
- [x] T020 [US3] Add a deterministic tile manifest model with diagram identity, canvas bounds, tile ordering, and overlap in `src/BrownPants.Cli/Analysis/LocalFlowchartTiling.cs`.
- [x] T021 [US3] Render bounded PNG regions from the shared layout without exceeding per-tile raster limits in `src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs`.
- [x] T022 [US3] Add explicit tile size/overlap options, deterministic filenames, manifest writing, and path-safe output handling in `src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs`.
- [x] T023 [US3] Document tiled output naming, manifest interpretation, reassembly expectations, and safety limits in `README.md` and `specs/068-polished-local-flowcharts/contracts/diagram-cli.md`.

## Phase 6: User Story 4 - Exchange editable diagrams (Priority: P4)

**Goal**: Interoperate with the pinned diagrams.net format baseline and report unsupported constructs.

**Independent Test**: Run every provenance-tracked corpus sample through import/export and verify the supported semantics/geometries survive or a precise unsupported diagnostic is returned; verify AC-005/AC-006 and SC-006.

- [x] T024 [P] [US4] Add a provenance-tracked diagrams.net compatibility corpus and expected results in `tests/BrownPants.Tests/Fixtures/DrawioCompatibility/`.
- [x] T025 [P] [US4] Add corpus round-trip and unsupported-construct tests in `tests/BrownPants.Tests/LocalFlowchartTests.cs`.
- [x] T026 [US4] Implement bounded, safe XML import for declared supported elements and reject unsafe or unsupported constructs explicitly in `src/BrownPants.Cli/Analysis/LocalFlowchartDrawioParser.cs`; expose import through `diagram --input-format drawio`.
- [x] T027 [US4] Preserve supported symbol, label, connection, page, style, node geometry, and edge waypoint data through export in `src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs`.
- [x] T028 [US4] Document the pinned upstream format-documentation revision, corpus provenance/coverage, fidelity boundaries, and unsupported constructs in the feature contract and `README.md`.

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T029 Run and record a five-reviewer usability check against the simple/elegant/usable/useful rubric (at least four score every dimension at least 4/5 and can follow the primary path at the stated scale), then publish the reviewed showcase output and instructions in `README.md` and `docs/`.
- [x] T030 Run build, focused flowchart tests, full solution tests, acceptance-hash validation, acceptance-evidence validation, and quickstart scenarios; record command results in `specs/068-polished-local-flowcharts/verification.md`.
- [x] T031 Complete `specs/068-polished-local-flowcharts/acceptance-evidence.json` with criterion mappings and reviewable evidence paths for all completed tasks.
- [x] T032 [US1] Document supported chart families, required source relationships, and alternatives for unsupported requests in CLI help, README, and agent instructions.
- [x] T033 [US1] Add small-format readability checks and a large showcase scenario whose output can be reviewed with the tile manifest, stable tile order, and cross-tile continuity cues.
- [x] T034 [US1] Design and implement evidence-backed organization-chart and brainstorm-map inputs using the shared renderer, with distinct layout and shape conventions and explicit rejection of unsupported or ambiguous relationships.
- [x] T035 [US1] Add compact and large chart fixtures, regenerate reviewed PNG examples, and verify AC-015/AC-016 and SC-011 against actual command outputs.

## Dependencies & Execution Order

- T001/T002 are setup and can begin independently.
- T003-T005 establish shared test and contract foundations before story work.
- US1 (T006-T012) is the visual MVP; US2 (T013-T018) adds source-backed caller and QA diagrams; US3 (T019-T023) adds tiling; US4 (T024-T028) defines diagrams.net interchange.
- US2 depends on semantic call graph evidence; US3 depends on US1's shared layout representation; US4 can start corpus preparation alongside US1, but import/export depends on a stable symbol profile.
- Polish depends on the desired stories being complete.

## Implementation Strategy

Deliver US1 first: a beautiful readable showcase chart with geometry checks and stakeholder review. Continue with source-backed caller/QA views, then tiling, then the versioned diagrams.net interoperability contract. Keep the offline/no-remote-resource behavior, limits, deterministic bytes, and honest support claims in every story.

- [x] T036 [US1] Save a diagrams.net XML sidecar for every successful PNG render, with paired overwrite/path-safety checks and deterministic sidecar tests.
- [x] T037 [US3] Index graph adjacency during rank assignment and crossing reduction; remove area-only layout rejection so large canvases remain eligible for bounded tiling, with a 5,200-node sparse-chain regression (FR-007, SC-003).
- [x] T038 [US3] Raise single-PNG capacity to 90% of the actual RGB byte-array limit, stream scanlines during compression, and test the 10% PNG-dimension/buffer boundaries and rendering beyond the old 4,096-pixel edge cap (FR-007, SC-003).
- [x] T039 [US3] Automatically tile images beyond single-raster capacity; remove the tile-count ceiling and stage PNG/XML artifacts one at a time while streaming the manifest, with tests for auto-selection and lazy enumeration beyond 1,024 tiles (FR-007, SC-003).
- [x] T040 [US3] Build one deterministic spatial index per layout and use tile-local node/edge candidates during rasterization, with a sparse-chain locality regression (FR-007, SC-003).
- [x] T041 [US3] Verify every tile query includes all padded node, routed-edge, and edge-label bounds that intersect the tile across the representative flow fixture (FR-007, SC-003).
- [x] T042 [US2] Keep method labels compact while retaining full compiler identity, source path, relationship, and hop depth in editable XML tooltips; render Unicode labels safely and preserve them in XML (AC-009, AC-017).
- [x] T043 [US1] Add hashed label-fit acceptance evidence, then reserve node/edge label bounds during layout, prevent label collisions and truncation, and reject unfit labels before output (AC-018, FR-022, SC-013).
- [x] T044 [US3] Group every tiled chart bundle in an output-stem subfolder for flow and caller charts; preserve unrelated files, remove stale manifest-listed tiles on overwrite, and validate the folder paths in the manifest (AC-019, FR-023, SC-014).
- [x] T045 [US3] Skip crossing-reduction adjacency construction when each rank has at most one node; extend the sparse-chain layout regression to 25,000 nodes and record measured test evidence (FR-007, SC-003).
- [x] T046 [US1] Diagnose remaining orthogonal connector crossings with bounded spatial queries, report crossings or the analysis limit in CLI output, and test both diagnostics without changing connections (AC-020, SC-015).
- [x] T047 [US3] Stream tiled editable-source XML directly to staged storage instead of buffering the full document in memory; preserve byte-identical deterministic output, reuse the computed layout, and verify the destination stream remains open (FR-007, SC-003).
- [x] T048 [US1] Audit the chart pipeline and dependency graph against the native managed .NET requirement; document the approved implementation boundary and verify there is no rendering toolkit, platform-native drawing API, external process, or network call (AC-021, FR-024, SC-016).
- [x] T049 [US4] Serialize computed route waypoints from the shared layout into every editable edge, preserving declared terminal IDs; verify forward, return, and long-edge paths against generated XML (AC-023, FR-025, SC-018).
- [x] T050 [US2] Keep unresolved invocation, constructor, and delegate-target counts distinct in caller-chart coverage diagnostics; verify a delegate-only gap is described accurately and marked incomplete (AC-010, FR-014).
- [x] T051 [US2] Add a local BrownPants-on-BrownPants QA script that compares an exact caller chart's symbols, callers, call-site paths/lines, and edges against a separate `impact --format json` report; record the observed coverage gaps (AC-024, FR-026, SC-019).
- [x] T052 [US2] Order caller-chart methods and call sites by caller depth so acyclic caller paths flow toward the requested target; verify the representative call tree avoids connector crossings and preserves the call direction (AC-001, AC-007, SC-001).
- [x] T053 [US2] Add small C# and Visual Basic methods containing nested decisions, pattern/switch cases, loops, early returns, explicit throws, and try/catch/finally; assert Roslyn CFG block and branch expectations in tests.
- [x] T054 [US2] Extend semantic analysis to produce a source-scoped Roslyn `ControlFlowGraph` snapshot for one exact method/constructor, preserving block operations, condition values, successor ordinals, branch semantics, and source lines; report unsupported CFG construction without fallback guesses.
- [x] T055 [US2] Map every CFG basic block and successor to a compact behavior chart with true/false and exit semantics, readable line references, and full compiler/source details in editable XML tooltips; make unsupported implicit exception behavior explicit.
- [x] T056 [US2] Expose `brownpants chart flow` for exact C# and Visual Basic method symbols using the existing offline PNG/diagrams.net writer, direction/tiling/overwrite rules, and an evidence-limited plain-English summary.
- [x] T057 [US2] Verify C# and Visual Basic behavior charts against Roslyn CFG fixtures, deterministic PNG/XML, CLI diagnostics, Release build, full tests, and acceptance hash/evidence checks; record a compact BrownPants QA example.
- [x] T058 [US1] Keep PNG arrowhead tips visible above supported target-shape borders, use the connector color consistently, and verify all supported shapes with pixel-level tests (AC-027, SC-021).
