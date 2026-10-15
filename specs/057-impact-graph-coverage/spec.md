# Feature Specification: Impact Graph Coverage

**Feature Branch**: `057-impact-graph-coverage`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Keep changed-file caller relationships available to impact analysis when repository-wide resolved-call output exceeds the display sample. Retain a separately bounded internal graph, report when that graph itself truncates, and preserve deterministic managed method and constructor impact behavior without changing the public semantic output sample.

## User Scenarios & Testing

### User Story 1 - Find callers beyond the semantic display sample (Priority: P1)

A maintainer changes a method in a later-loaded project in a large repository. They need `impact` to find its resolved callers even when unrelated projects filled the bounded semantic report sample first.

**Why this priority**: A zero-caller result caused by sample order can hide real QA and implementation impact in brownfield repositories.

**Independent Test**: Analyze a fixture where unrelated resolved calls precede a caller of the changed method and the display sample is smaller than the call set. `impact` must return the caller while the public semantic call sample remains bounded.

**Acceptance Scenarios**:

1. **AC-001** **Given** a changed managed method with a resolved caller beyond the public semantic sample, **When** impact is analyzed, **Then** the caller is returned with its path, line, and depth.
2. **AC-002** **Given** the call graph remains under its independent bound, **When** impact is analyzed, **Then** display-sample omissions alone do not remove relevant callers or make the report claim that the graph itself was truncated.
3. **AC-003** **Given** method and constructor callers, **When** impact is analyzed, **Then** both relationship kinds remain traceable using the same shortest-depth and cycle rules.

### User Story 2 - Recognize when the impact graph itself is bounded (Priority: P2)

A maintainer needs an explicit warning when a very large semantic graph exceeds the separate impact-analysis bound, so an absent caller is not mistaken for evidence of no impact.

**Why this priority**: Separating the output sample from traversal improves coverage, while a hard graph bound keeps memory predictable and requires honest truncation reporting.

**Independent Test**: Use a small injectable graph ceiling in a test fixture, exceed it, and verify the report is incomplete with a retained/omitted count and no complete no-caller conclusion.

**Acceptance Scenarios**:

1. **AC-004** **Given** the internal impact graph reaches its configured bound, **When** impact is analyzed, **Then** the report is incomplete and identifies the omitted relationship count.
2. **AC-005** **Given** the internal impact graph does not reach its bound, **When** the public semantic display sample is smaller than the complete graph, **Then** impact completeness is based on the internal graph and other actual evidence limits.
3. **AC-006** **Given** semantic relationships are analyzed for impact, **When** a semantic inventory is also reported, **Then** its existing JSON fields and caller-selected public sample limit remain unchanged.

## Edge Cases

- A changed file has no method declarations in the loaded graph.
- The public semantic sample omits all calls to the changed declarations, while the internal graph retains them.
- The internal graph bound is reached before or during the changed method's incoming edges.
- Constructor and invocation edges share targets, form cycles, or lead to duplicate caller locations.
- The semantic workspace reports unresolved invocations or project-loading issues independently of graph sampling.

## Requirements

### Functional Requirements

- **FR-001**: Impact analysis MUST traverse a separately retained, bounded set of resolved method and constructor relationships rather than relying only on the public semantic display sample.
- **FR-002**: A resolved relationship omitted from the public display sample MUST remain available to impact traversal while it is retained in the internal impact graph.
- **FR-003**: Reaching the internal impact-graph bound MUST record the number of omitted relationships, mark impact incomplete, and prevent a complete no-impact conclusion.
- **FR-004**: Public semantic output MUST retain its existing caller-selected sample limit and schema; the internal impact graph MUST NOT be serialized as a second unbounded report surface.
- **FR-005**: Method and constructor traversal MUST preserve deterministic ordering, shortest discovered depth, cycle protection, and existing evidence classification.
- **FR-006**: Workspace failures and unresolved calls MUST remain explicit and independent from display-sample omissions and internal graph truncation.

## Success Criteria

- **SC-001**: A fixture with `--limit 1` and a relevant caller placed after unrelated resolved calls reports that caller in impact while retaining only one public semantic edge.
- **SC-002**: A fixture with an intentionally small internal graph ceiling retains no more than that ceiling, reports its exact omitted-edge count, and returns `IsComplete: false`.
- **SC-003**: BrownPants-on-BrownPants impact at `--limit 10000` reports managed callers to the Bug Lab workflow even though the repository has more than 10,000 resolved relationships; existing unresolved and workspace issues remain visible.
- **SC-004**: Repeated fixture analysis produces identical ordered JSON and Markdown impact reports.
- **SC-005**: Existing method, constructor, mixed native/managed, and short-graph impact tests continue to pass without changing their relationship semantics.

## Assumptions

- The public `--limit` continues to bound displayed semantic samples and final impact findings.
- A separate fixed implementation ceiling bounds retained in-memory impact edges; its value is documented in the implementation plan and can be lowered in tests without changing CLI behavior.
- Semantic impact continues to use the existing explicit MSBuildWorkspace path; this feature does not make project evaluation part of default inspection.
- Unresolved dynamic, reflection, runtime, and native relationships remain outside the compiler-resolved managed graph.
