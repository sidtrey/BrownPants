# Feature Specification: Polished Local Flowcharts

**Feature Branch**: `068-polished-local-flowcharts`

**Created**: 2026-10-15

**Status**: Draft

**Input**: User description: Improve BrownPants' local flowchart rendering into polished, standards-informed diagrams with strong layouts, collision handling, large tiled PNG output, and diagrams.net interoperability. Investigate established flowchart conventions from IBM and standards bodies.

## User Scenarios & Testing

### User Story 1 - Read a polished flowchart (Priority: P1)

As a developer or reviewer, I want a flowchart generated from a concise description to be readable and visually organized, so I can understand a system without manually redrawing it.

**Why this priority**: Readability is the core user value and the feature's demonstration point.

**Independent Test**: Render representative diagrams locally and review PNGs at their documented viewing scale.

**Acceptance Scenarios**:

1. **AC-001** Given a branching workflow with long labels, **When** the user selects a supported layout direction, **Then** labels remain legible, shapes and labels do not overlap, and connectors do not pass through unrelated shapes.
2. **AC-002** Given the prepared showcase diagram, **When** it is reviewed by intended stakeholders using the four-part chart quality rubric, **Then** at least four of five rate every dimension at least 4 out of 5 and can follow the main path without a verbal walkthrough.
3. **AC-007** Given a cyclic graph and a dense branching graph, **When** rendered repeatedly from identical input, **Then** the output is byte deterministic, all nodes and edges remain represented, and routed edges avoid node interiors.
4. **AC-008** Given a diagram using a named flowchart symbol profile, **When** rendered, **Then** the shape meanings and direction conventions are documented and visibly distinguishable.
5. **AC-018** Given a diagram with long node labels, long edge labels, and dense connections, **When** BrownPants lays it out and renders it, **Then** it preserves every label without truncation, wraps text within the node or edge-label bounds, and avoids overlap between node text, node shapes, edge-label bounds, and unrelated connectors. If a label cannot fit at the documented minimum text size, BrownPants reports the affected label and does not emit a partial image bundle.
6. **AC-020** Given a layout with connector crossings, **When** the chart is rendered, **Then** BrownPants reports remaining crossings without changing declared connections; above the documented analysis limit it reports that crossing analysis was skipped.
7. **AC-021** Given a chart is laid out, rasterized, or exported, **When** BrownPants produces the result, **Then** these operations use BrownPants-managed C# and .NET base-class-library code without requiring a third-party charting toolkit, platform-specific rendering API, external process, or network service. Any future rendering dependency requires documented necessity and a trust/security review.
8. **AC-022** Given a representative chart is reviewed, **When** the rubric is applied, **Then** **simple** means purpose and primary flow are immediately apparent; **elegant** means consistent hierarchy, spacing, and restrained decoration; **usable** means labels and relationships remain legible and unobscured at the stated viewing scale; and **useful** means the chart answers its stated developer, QA, or team question and exposes evidence gaps.
9. **AC-023** Given BrownPants generates an editable diagrams.net file for a chart, **When** the file is opened or imported, **Then** every edge retains BrownPants' computed orthogonal route points as well as its declared source and target, so editing begins from the same geometry as the PNG.
10. **AC-024** Given BrownPants is charting a compiler-resolved method in its own repository, **When** the QA workflow compares the chart with an independent `impact --format json` report, **Then** the exact target symbol, caller set, and source call-site paths and lines agree, while unresolved relationships remain explicitly reported.
11. **AC-027** Given a connector targets any supported shape, **When** the PNG is rendered, **Then** the arrowhead tip is visible at the target perimeter and uses the same connector color as its line.

### User Story 2 - Generate source-backed call and QA diagrams (Priority: P2)

As a developer or QA reviewer, I want an agent to turn a request about a source method into a call tree, so I can see which methods lead to it and where testing may be relevant.

**Why this priority**: Brownfield impact evidence is a core BrownPants strength, and converting it into a readable diagram makes that evidence easier to use in reviews and test planning.

**Independent Test**: On a small C# fixture, request callers for an exact method and inspect the diagram's resolved call edges, source locations, depth, and coverage status.

**Acceptance Scenarios**:

1. **AC-009** Given a repository-relative source file and exact method target, **When** an agent requests a caller tree with semantic analysis explicitly enabled, **Then** the output includes the root method and every retained compiler-resolved direct/transitive caller with call-site path, line, and depth.
2. **AC-010** Given partial project loading, unresolved calls, or sampled graph edges, **When** a source graph is rendered, **Then** completeness and omissions are visible, and lexical candidates are not shown as compiler-resolved edges.
3. **AC-011** Given a QA-oriented request, **When** the agent requests a test-impact view, **Then** known test-path candidates are marked as candidates and are not presented as proof of runtime test coverage.
4. **AC-012** Given a natural-language request such as “show me everything that calls this function,” **When** the host agent maps it to the documented command and includes `--semantic` only when the user/policy permits project evaluation, **Then** the local command produces the requested diagram without BrownPants calling an LLM or network service.
5. **AC-013** Given a generated code or QA diagram, **When** it is returned to the team, **Then** it includes one to three short plain-English sentences describing what it shows, why it matters, and whether evidence is incomplete.
6. **AC-014** Given an unsupported chart request, **When** the host agent responds, **Then** it names supported chart types and offers a relevant alternative without inventing relationships.

---

### User Story 3 - Share large diagrams safely (Priority: P3)

As a team member in a restricted environment, I want large diagrams rendered locally into viewable PNG tiles, so I can review or share them without hosted services or a huge single image.

**Independent Test**: Render a graph that exceeds one tile and verify tile dimensions, stable names, complete coverage, and correct reassembly order using the generated manifest.

**Acceptance Scenarios**:

1. **AC-003** Given a graph larger than one tile, **When** tiled PNG output is requested, **Then** every diagram region appears in one or more bounded tiles with a manifest that identifies tile coordinates and overlap.
2. **AC-004** Given network access is unavailable, **When** a supported diagram is rendered, **Then** rendering succeeds without network access or remote assets.
3. **AC-016** Given both a compact diagram and a diagram too large to review as one image, **When** they are rendered, **Then** the compact result remains legible and the large result is split into ordered, bounded tiles with continuity cues and a manifest.
4. **AC-017** Given PNG output is requested, **When** rendering succeeds, **Then** a sibling editable .drawio file is saved with the image and follows the same overwrite and path-safety rules.
5. **AC-019** Given a chart is tiled automatically or by explicit tile-size, **When** output is generated, **Then** every tile, its editable sidecar, and the manifest are grouped in a dedicated subfolder named after the requested output stem; overwriting the bundle removes stale files listed by its prior manifest and preserves unrelated files.

### User Story 4 - Exchange editable diagrams (Priority: P4)

As a user working with diagrams.net, I want BrownPants to exchange editable diagrams with that tool while retaining supported diagram meaning and layout.

**Independent Test**: Open a compatibility corpus in diagrams.net, export it, import/export it through BrownPants, and compare supported semantics and geometry within documented tolerances.

**Acceptance Scenarios**:

1. **AC-005** Given a supported diagrams.net XML document, **When** it is imported and exported, **Then** supported shapes, labels, connections, and page geometry survive a documented round trip.
2. **AC-006** Given an unsupported XML feature, **When** import is attempted, **Then** BrownPants identifies the unsupported construct instead of silently discarding it.

### User Story 5 - Explain organization and idea maps (Priority: P2)

As a lead or facilitator, I want a clear organization chart or brainstorm map from relationships the team has supplied, so ownership and ideas are easy to discuss without manually drawing boxes.

**Independent Test**: Provide a small organization hierarchy and a nested brainstorm topic tree; verify distinct chart styling, readable labels, preserved parent-child links, and a concise plain-English explanation. Reject multiple managers, cycles, or unsupported relationships rather than guessing.

**Acceptance Scenarios**:

1. **AC-015** Given a request for a process flow, call/impact tree, organization chart, or brainstorm map, **When** the requested chart family is supported and its source provides the relationships, **Then** BrownPants renders it with a matching visual convention and concise explanation.

### User Story 6 - Explain what a source method does (Priority: P1)

As a developer or QA reviewer working in a brownfield codebase, I want a chart of one exact method's compiler-derived control flow, so I can follow its decisions, loops, exits, and explicit exception paths without manually reconstructing them from source.

**Independent Test**: Request a chart for exact C# and Visual Basic methods containing nested branches, switch/case logic, loops, early returns, and explicit throws; compare every CFG block and successor against Roslyn's control-flow graph and inspect the PNG and editable XML.

**Acceptance Scenarios**:

1. **AC-025** Given an exact C# or Visual Basic method symbol and repository-relative source file, **When** the user requests a local behavior chart with semantic analysis enabled, **Then** every Roslyn control-flow block and every non-null successor in scope is represented, with deterministic source line evidence and true/false or branch semantics attached to the correct connector.
2. **AC-026** Given a method containing loops, switch/pattern decisions, early returns, explicit throw/rethrow, or structured exception handling, **When** its behavior chart is generated, **Then** cycles and compiler-reported branch/exit semantics are preserved; behavior not represented by Roslyn's CFG (including implicit runtime exceptions) is clearly excluded from the summary, and unsupported or unavailable CFG data fails with an actionable diagnostic rather than a plausible-looking partial chart.
### Edge Cases

- Multiple disconnected components, rootless cycles, self-loops, parallel edges, and branches with unequal path lengths.
- Empty or whitespace labels, labels at the maximum supported length, and multiline/non-ASCII input.
- Graphs whose dimensions exceed a tile or output safety limit.
- Unknown symbol-profile identifiers and unsupported diagrams.net XML elements or style features.
- Diagrams with too many crossings to eliminate without changing graph meaning; report a layout-quality limitation rather than changing connectivity.

## Requirements

### Functional Requirements

- **FR-001**: Users MUST be able to render supported flow descriptions entirely on the local machine without a network service.
- **FR-002**: The renderer MUST provide documented top-down and left-to-right layouts and MUST produce deterministic output for identical inputs and options.
- **FR-003**: The renderer MUST avoid unintended overlap between nodes and labels, keep labels within their shapes or explicitly attached to edges, and route connectors so they do not cross unrelated node interiors.
- **FR-004**: The layout MUST preserve every declared node and edge, including cycles and disconnected components; layout optimization MUST NOT alter graph connectivity.
- **FR-005**: The renderer MUST define a standards-informed symbol profile with named symbols, meanings, and flow direction conventions. Documentation MUST distinguish profile guidance from verified conformance to a published standard.
- **FR-006**: Standards research MUST prioritize software flowchart conventions such as ISO 5807 and historical IBM flowcharting guidance. Each adopted rule MUST be traceable to a source; symbol rules that cannot be verified or reused must be described without claiming conformance.
- **FR-007**: Users MUST be able to generate large diagrams as deterministic PNG tiles with a manifest that records tile order, bounds, overlap, and source diagram identity.
- **FR-008**: PNG and diagrams.net XML generation MUST work offline and MUST not load scripts, images, fonts, or other resources from network locations.
- **FR-009**: BrownPants MUST define a pinned diagrams.net format-documentation baseline, supported file variants/features, and round-trip fidelity. Import/export claims MUST be limited to a provenance-tracked regression corpus and explicitly tested constructs; unsupported constructs MUST be reported.
- **FR-010**: Dependencies added for rendering or XML support MUST be necessary, maintained, license-compatible, and reviewed for known vulnerabilities before adoption; prefer existing platform capabilities when they meet requirements.
- **FR-011**: Existing `diagram` command behavior MUST remain compatible unless an option or output change is explicitly documented.
- **FR-012**: Documentation MUST include representative dense branching, long-label, cyclic, and multi-tile outputs and state their viewing scale and known limits.
- **FR-013**: Users and agents MUST be able to request a source-backed caller graph from an exact repository-relative source file and method target, with direction, depth/sample bounds, output path, and an explicit semantic-analysis opt-in stated.
- **FR-014**: Call graphs MUST include compiler-resolved call-site paths, line numbers, and hop depths, and preserve whether the analysis is complete, sampled, or partial.
- **FR-015**: QA-oriented diagrams MUST mark test paths as candidates and MUST NOT claim runtime coverage.
- **FR-016**: Host-facing instructions MUST show how to translate a natural-language diagram request into the deterministic local CLI; BrownPants MUST NOT invoke an LLM or network service to interpret the request.
- **FR-017**: Every generated code or QA chart MUST be accompanied by a plain-English summary of at most three short sentences covering its subject, practical relevance, and evidence completeness.
- **FR-018**: Host-facing instructions MUST describe supported chart requests and explain unsupported requests with supported alternatives; the host MUST NOT fabricate source relationships.
- **FR-019**: The documented chart catalog MUST distinguish supported process flows, call/impact trees, organization charts, and brainstorm maps, identify the required source evidence for each, and label future or unsupported requests clearly.
- **FR-020**: Representative small and large chart examples MUST demonstrate readable compact output and tiled large output with stable ordering, boundaries, continuity cues, and a manifest.
- **FR-021**: Every successful PNG render MUST also save an editable diagrams.net XML sidecar beside the image; both outputs MUST use the same diagram source, stay within the repository, and honor explicit overwrite protection.
- **FR-022**: The layout MUST reserve measurable bounds for every node and edge label. It MUST preserve full label text, wrap deterministically, prevent label collisions with shapes, other labels, and unrelated connectors, and report any unfit label before writing output.
- **FR-023**: Tiled output MUST be grouped under a dedicated, repository-relative directory named after the requested output stem. Replacing a bundle MUST be transactional, remove stale generated tiles listed in its prior manifest, and preserve unrelated files in the folder.
- **FR-024**: Chart layout, rasterization, PNG encoding, and diagrams.net XML generation MUST remain implemented in managed C# using .NET platform capabilities wherever feasible. A chart-specific third-party package, platform-native renderer, or external process MUST NOT be introduced unless a documented requirement cannot reasonably be met by the existing .NET capabilities and its trust, security, and maintenance are reviewed.
- **FR-025**: Generated diagrams.net XML MUST serialize each laid-out edge's computed route points, including for locally authored charts, while preserving its source and target identities.
- **FR-026**: The chart QA workflow MUST compare a real BrownPants caller chart with an independently emitted call-impact report and fail on missing, extra, or incorrectly located call-site relationships.
- **FR-027**: `chart flow` MUST use Roslyn `ControlFlowGraph` for an exact C# or Visual Basic method/constructor and MUST map every retained basic block and non-null successor to local flowchart nodes and edges without changing branch destinations.
- **FR-028**: Behavior charts MUST retain compiler branch condition, branch semantics, and repository-relative source lines in labels or editable XML details; unsupported CFG construction, missing blocks, or omitted successors MUST be reported as failure/coverage gaps. Summaries MUST distinguish explicit CFG exception flow from implicit runtime exceptions and MUST NOT claim to model behavior absent from the CFG.

### Key Entities

- **Flow description**: The user's declared nodes, labels, symbol meanings, and directed connections.
- **Symbol profile**: A named set of flowchart symbols and conventions with source attribution and documented limits.
- **Layout**: Deterministic node placement, edge routing, direction, spacing, and page bounds.
- **Tile manifest**: Ordered tile bounds and overlap needed to review or reassemble a large diagram.
- **Compatibility corpus**: Provenance-tracked diagrams.net XML samples and expected supported round-trip behavior.
- **Source call graph**: A target method plus compiler-resolved caller/callee edges, call-site locations, depths, test-path candidate markers, and completeness/omission evidence.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All representative fixtures (dense branching, long labels, cycles, disconnected components, and mixed path lengths) render without node or label overlap and without connectors crossing unrelated node interiors.
- **SC-002**: Repeated rendering of identical fixtures and options produces byte-identical PNGs, tiles, manifests, and supported XML exports on the same supported runtime.
- **SC-003**: Every pixel of an oversized fixture is represented by the generated tile set, and tile bounds, order, and overlap are mechanically verifiable from its manifest.
- **SC-004**: At least 4 of 5 reviewers in a documented usability review can correctly follow the primary path in representative diagrams at the stated viewing scale without zooming beyond the documented default.
- **SC-005**: Every adopted symbol convention has a source citation and a test or visual fixture demonstrating the convention; no unsupported standards-compliance claim appears in documentation.
- **SC-006**: Every construct in the declared diagrams.net compatibility corpus either round-trips within documented semantic/geometry tolerances or produces an explicit unsupported-feature diagnostic.
- **SC-007**: In a recorded review of the showcase diagram, the intended stakeholder rates its visual quality at least 4 out of 5 and follows the main path without a verbal walkthrough.
- **SC-008**: A complete source graph contains the requested method and every retained resolved call relationship within the declared depth/sample limits, with stable source locations and hop depths.
- **SC-009**: Incomplete semantic coverage, unresolved calls, and omitted edges are visibly reported and never represented as compiler-resolved relationships.
- **SC-010**: At least 4 of 5 reviewers can state the chart purpose, key impact, and evidence completeness within 30 seconds using the diagram and its plain-English summary.
- **SC-011**: The published examples include one compact chart that remains readable at the stated display size and one oversized chart whose tiles and manifest are mechanically checked for complete, ordered coverage.
- **SC-012**: Every successful PNG fixture produces a parseable, deterministic `.drawio` sidecar that contains every declared node and relationship; a conflicting sidecar prevents either output from being replaced.
- **SC-013**: Label stress fixtures covering maximum-length words, multiword labels, narrow symbols, and dense edge annotations pass geometry checks for complete text bounds, no node/edge-label overlap, no label-to-label overlap, and no label intersection with unrelated connectors.
- **SC-014**: Both diagram and caller-chart tiled outputs place all manifest paths under `<output-stem>/`; overwrite removes obsolete manifest-listed tiles, preserves unrelated files, and leaves no partial bundle after a collision or failure.
- **SC-015**: A crossing-route fixture produces a stable diagnostic while retaining every edge, and a graph above the analysis limit reports that crossing analysis was skipped.
- **SC-016**: A source and dependency audit confirms the chart pipeline has no charting toolkit dependency, platform-specific rendering API, external process, or network call; the CLI's chart path runs using its managed implementation and .NET platform libraries.
- **SC-017**: Five reviewers score representative compact and complex charts against all four rubric dimensions; at least four score every dimension at least 4/5 and correctly state the chart's purpose and primary takeaway without a verbal walkthrough.
- **SC-018**: A generated fixture containing forward, return, and long edges serializes the exact intermediate coordinates from the PNG layout into each editable XML edge; repeated exports are byte-identical and preserve all source/target IDs.
- **SC-019**: A local BrownPants-on-BrownPants QA command completes call-impact analysis and caller-chart generation, then verifies exact agreement for the selected target, direct callers, call-site paths/lines, and represented edge count; gaps from unresolved delegates or capped traversal remain visible.
- **SC-020**: C# and Visual Basic fixtures with nested `if`/`else`, switch/case and pattern guards, loops, multiple returns, explicit throws, and `try`/`catch`/`finally` produce charts whose block IDs, branch destinations, branch values, exit semantics, and source lines match the Roslyn CFG exactly; repeated PNG and `.drawio` exports are deterministic.
- **SC-021**: Pixel-level tests verify arrowhead visibility and connector-color consistency for every supported target shape; the matching editable XML retains its source and target relationships.

## Assumptions

- The existing `brownpants-flow` description remains the primary authoring format for the initial renderer improvements; broad diagram import is separately scoped by the declared diagrams.net compatibility contract.
- ISO 5807 and IBM historical guidance are research inputs, not blanket certification requirements. NIST FIPS PUB 24 may inform historical context when relevant, but must not be presented as current regulatory guidance.
- A polished result is evaluated using reproducible visual fixtures and reviewer feedback; claims of superiority to Visio require comparative evidence and are not acceptance criteria by themselves.
- The product remains offline-first and avoids new rendering dependencies unless the plan shows that existing capabilities cannot satisfy the requirements.
