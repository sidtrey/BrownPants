# Feature Specification: Nested Callable Identities

**Feature Branch**: `065-nested-callable-identities`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Make compiler-resolved call evidence identify calls made inside lambdas, anonymous methods, and local functions so brownfield call traces do not show empty or colliding caller names.

## User Scenarios & Testing

### User Story 1 - Identify calls inside nested C# callables (Priority: P1)

A maintainer follows a DI registration or factory callback and needs to know which enclosing method and nested callable contains each resolved call.

**Why this priority**: Calls inside common DI factory lambdas currently appear under a caller identity with an empty method name, obscuring the actual origin and risking graph-edge collisions.

**Independent Test**: Compile a small C# fixture with multiple lambdas, an anonymous method, and a local function; inspect each resolved call's caller identity and follow an impacted method through those edges.

**Acceptance Scenarios**:

1. **AC-001** Given a resolved call inside a lambda, when semantic analysis runs, then its caller identity includes the enclosing named method and a stable repository-relative lambda location, with no empty method-name segment.
2. **AC-002** Given two lambdas in one method and multiple calls inside one lambda, when semantic evidence is emitted, then the two lambdas have distinct identities while calls in the same lambda share one identity.
3. **AC-003** Given calls inside an anonymous method or local function, when semantic analysis runs, then caller identity retains the enclosing method and identifies the anonymous/local callable; local-function call targets and declarations use the same identity.

### User Story 2 - Preserve readable identities across managed languages (Priority: P2)

A team with C# and Visual Basic code needs consistent call-origin evidence in semantic reports and impact traces.

**Why this priority**: BrownPants analyzes both languages in mixed legacy repositories, and nested callable evidence should remain useful across both.

**Independent Test**: Compile a VB fixture with a lambda and verify the caller identity, serialized JSON, and downstream impact edge.

**Acceptance Scenarios**:

1. **AC-004** Given a resolved Visual Basic call inside a lambda, when semantic analysis runs, then its caller identity identifies the enclosing method and a stable lambda location without an empty method-name segment.
2. **AC-005** Given identical sources analyzed repeatedly, when Markdown or JSON is emitted, then caller identities, graph edges, and ordering remain deterministic and existing method/constructor targets remain unchanged.

### Edge Cases

- Nested lambdas, lambdas inside local functions, and multiline lambda bodies.
- Multiple anonymous callables on one source line.
- Calls in generated or linked source whose syntax tree path is outside the repository.
- Generic enclosing methods and local functions.
- C# and VB method overloads with otherwise identical nested callable positions.

## Requirements

### Functional Requirements

- **FR-001**: Compiler-resolved calls MUST identify the callable context that contains the call, including anonymous and local callable scopes.
- **FR-002**: Anonymous callable identities MUST include their nearest named enclosing method and a deterministic source location; repository paths MUST be relative and external absolute paths MUST NOT be exposed.
- **FR-003**: Distinct anonymous callables MUST have distinct identities; calls within one anonymous callable MUST share its identity.
- **FR-004**: Local-function declaration identities, calls to those functions, and calls contained within them MUST preserve their enclosing callable context consistently.
- **FR-005**: C# and Visual Basic semantic evidence MUST apply the identity rules to supported lambdas and retain resolved target identity, target location, and relationship kind.
- **FR-006**: Markdown and JSON semantic evidence and call-impact traversal MUST expose the corrected caller identity without dropping or merging relationships.
- **FR-007**: Existing named method and constructor identities, report schema, determinism, and evidence classification MUST remain compatible.
- **FR-008**: The change MUST remain source-only, deterministic, offline, and independent of project builds beyond the existing opt-in semantic workspace.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The Bug Lab `AddTransient` lambda call sites show `OrderBatchFactory.Register` plus distinct source-located callable context instead of `OrderBatchFactory.()`.
- **SC-002**: C# fixtures demonstrate unique lambda/anonymous identities, shared identity for multiple calls in one lambda, and parent-qualified local-function identities with matching targets.
- **SC-003**: A VB lambda fixture reports a readable caller identity and retains its resolved invocation and impact edge.
- **SC-004**: Repeated semantic JSON/Markdown output is byte-stable; existing named method and constructor identity tests continue to pass.
- **SC-005**: Release build and full test suite pass without reducing resolved invocation/constructor counts or creating false unresolved relationships.

## Assumptions

- A source location is an acceptable stable identity anchor for anonymous callables because they have no user-defined method name.
- For source outside the repository, identity uses a redacted external marker plus line and column, not an absolute path.
- This feature corrects caller identity strings; it does not infer delegate execution or runtime callback ordering.
