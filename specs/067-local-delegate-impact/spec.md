# Feature Specification: Local Delegate Impact

**Feature Branch**: `067-local-delegate-impact`

**Created**: 2026-10-15

**Status**: Draft

**Input**: Improve brownfield call tracing where a local delegate is assigned a method group or lambda and invoked later. Roslyn currently resolves the invocation to `Delegate.Invoke`, which does not identify the underlying source callable.

## User Scenarios & Testing

### User Story 1 - Trace a locally bound callback (Priority: P1)

A maintainer follows a callback or method group in unfamiliar C# or VB code and needs impact analysis to reach the source method actually assigned to the local delegate.

**Why this priority**: Delegate callbacks are common in factories, event-driven code, and legacy application wiring. Reporting only `Action.Invoke` can hide the affected source method and its callers.

**Independent Test**: Compile small C# and VB fixtures with a local delegate bound to a method group or lambda, invoke it, and trace from the target source file to the invocation caller.

**Acceptance Scenarios**:

1. **AC-001** Given a local delegate initialized to one compiler-bound method group or lambda and used only as a local invocation, when semantic analysis runs, then the invocation edge identifies the assigned method or anonymous callable and retains the invocation path, line, and enclosing caller.
2. **AC-002** Given overloaded methods assigned to a delegate, when the delegate is invoked, then the compiler-selected overload is reported and impact from that target reaches the delegate caller with `relationshipKind: delegate`.
3. **AC-003** Given a delegate initialized to a lambda whose body calls a changed method, when impact runs, then tracing can continue from the lambda callable to the enclosing local-delegate caller.

### User Story 2 - Make unsupported delegate flow visible (Priority: P2)

A maintainer analyzes a callback that is reassigned, escapes its method, is captured, or is supplied through a field or parameter and needs BrownPants to avoid guessing its target.

**Independent Test**: Compile fixtures with multiple assignments, reassignment, capture, and parameter-provided delegates; verify none is falsely connected to a method-group target and the analysis reports the unresolved delegate target.

**Acceptance Scenarios**:

1. **AC-004** Given a delegate invocation whose target cannot be safely resolved to one local method group or lambda, when semantic analysis runs, then it remains distinct from a resolved source call, increments a bounded unresolved-delegate count/sample, and makes managed impact incomplete.
2. **AC-005** Given repeated analysis of the same sources, when Markdown and JSON are emitted, then delegate relationships, unresolved delegate samples, ordering, and existing method/constructor counts remain deterministic and compatible.

### User Story 3 - Prove the value in a matched task (Priority: P1)

A team comparing its usual SDD workflow with the same workflow assisted by BrownPants needs an auditable result before describing BrownPants as better.

**Independent Test**: Run three isolated, matched agent pairs against the same frozen method-group bug task and score the final structured findings against a source-derived answer key.

**Acceptance Scenarios**:

1. **AC-006** Given a frozen bug task that requires tracing a method-group delegate, when three fresh matched runs use the same source snapshot, task prompt, model, CLI, sandbox, and result schema with BrownPants evidence as the sole condition difference, then the pooled assisted result improves actionable managed-impact recall by at least 20 percentage points without reducing precision, at least two pairs improve recall, and both conditions' repairs pass the same independent regression check. Raw runs, usage, scores, hashes, and limitations are retained; otherwise the README and release notes MUST report that superiority was not demonstrated.

## Edge Cases

- Explicit `callback.Invoke()` and shorthand `callback()` syntax.
- Generic and overloaded method groups, local functions, and lambdas.
- Delegate locals with more than one invocation but one immutable local assignment.
- Reassignment, ref/out use, passing the local as an argument, return/field storage, and closure capture.
- Metadata-only targets and external source paths remain without repository target locations.

## Requirements

### Functional Requirements

- **FR-001**: Semantic analysis MUST identify local delegate invocation expressions bound by Roslyn to `Delegate.Invoke`.
- **FR-002**: A delegate invocation MAY be linked to a method group or lambda only when one local declaration provides the target and all uses are local delegate invocations in the same callable; overload selection MUST come from Roslyn.
- **FR-003**: Safe delegate links MUST use the canonical callable identity, source target location when repository-backed, enclosing caller, and a distinct `delegate` relationship kind so impact traversal can continue through the callback.
- **FR-004**: Reassigned, captured, escaped, field-backed, parameter-backed, or otherwise ambiguous delegate invocations MUST NOT be connected to an inferred source target.
- **FR-005**: Unsupported delegate targets MUST be reported with an exact count and bounded source-located samples; managed impact MUST be marked incomplete when such targets remain.
- **FR-006**: Invocation syntax totals and existing method/constructor counts MUST retain their current meanings. Delegate target certainty MUST be reported separately from the compiler-resolved `Delegate.Invoke` symbol.
- **FR-007**: C# and Visual Basic behavior MUST be deterministic, repository-relative, and must not expose external absolute paths or imply runtime execution.

## Success Criteria

### Measurable Outcomes

- **SC-001**: C# method-group and lambda fixtures plus a VB method-group fixture each trace from the assigned callable to the local delegate invocation without adding duplicate invocation counts.
- **SC-002**: An overloaded method-group fixture reports only Roslyn's selected overload and impact returns the correct enclosing caller.
- **SC-003**: Reassigned, captured, escaped, and parameter-provided delegate fixtures produce no false source target, report unresolved delegate evidence, and make impact incomplete.
- **SC-004**: Existing direct method, constructor, local-function, nested-callable, and Bug Lab managed/native impact results continue to pass; Release build and full test suite pass.
- **SC-005**: Repeated JSON/Markdown output is byte-stable, and external target paths remain redacted.
- **SC-006**: Three matched baseline/assisted Codex runs are executed in isolated copies with counterbalanced condition order, identical frozen inputs, source-derived scoring, and no answer-key exposure. Pooled actionable managed-impact recall improves by at least 20 percentage points with no precision loss, at least two paired trials favor BrownPants, and all six post-fix checks pass. The full protocol and raw output are retained; if the threshold is missed, no advantage claim is made.

## Assumptions

- Single-assignment local delegate bindings can be verified conservatively from Roslyn symbols and local syntax/data flow without evaluating the project or runtime.
- A delegate target edge is source-level impact evidence, not proof the callback executes.
- General interprocedural points-to analysis, event subscription activation, reflection, and container-managed delegate lifetimes remain out of scope.
