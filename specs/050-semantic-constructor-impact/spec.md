# Feature Specification: Semantic Constructor Impact

**Feature Branch**: `050-semantic-constructor-impact`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Extend BrownPants' brownfield semantic impact graph to cover constructor definitions and compiler-resolved object creation and constructor chaining. Factory and dependency-injection code commonly connects through constructors, while current call analysis only walks C#/VB invocation expressions.

## User Scenarios & Testing

### User Story 1 - Trace constructor use in an unfamiliar codebase (Priority: P1)

A developer changes a constructor in a legacy C# or VB project. They need to find source-located construction sites, distinguish overloaded constructors, and trace affected callers and test-path candidates without treating container registration or reflection as proven execution.

**Independent Test**: Analyze temporary C# and VB compilations containing overloaded constructors, target-typed creation, constructor chaining, method calls, unresolved construction, and decoy constructors. Compare exact symbols and locations, then run impact analysis from a changed constructor declaration.

**Acceptance Scenarios**:

1. **AC-001** Given C# or Visual Basic source with explicit constructors, when semantic analysis runs, then compiler-bound object creation and constructor chaining are reported with the exact constructor signature and source target location when available; C# target-typed `new()` is included.
2. **AC-002** Given a changed constructor declaration, when semantic impact runs, then direct and transitive construction callers are included with a distinct `constructor` relationship kind, without merging them with method invocation counts or same-name overloads.
3. **AC-003** Given an unresolved or ambiguous construction, when the report is emitted, then it remains unresolved with candidate reason/targets and makes impact completeness false; unsupported DI container, reflection, and native activation paths are listed as limits rather than inferred edges.
4. **AC-004** Given a repository where semantic analysis was not requested, when inventory runs, then existing static behavior remains unchanged and no constructor edges are presented as resolved.
5. **AC-005** Given existing method-call analysis, when constructor support is added, then method invocation counts, identities, target lines, and serialized fields remain available with stable ordering; reports expose the new relationship kind and constructor counts.

## Requirements

- **FR-001**: Resolve explicit C# and VB constructor declarations and compiler-bound object creation/chaining syntax using Roslyn symbols.
- **FR-002**: Include C# explicit and target-typed object creation, `this(...)`/`base(...)` initializers, and VB `New` construction when compiler binding resolves a constructor.
- **FR-003**: Preserve overload identity, caller symbol, call-site path/line, target signature, and target path/line when the target is in the repository.
- **FR-004**: Keep constructor syntax/resolved/unresolved counts separate from method invocation counts; report bounded unresolved constructor candidates and semantic completeness limits.
- **FR-005**: Extend impact traversal to resolved constructor edges and identify the relationship kind in JSON and Markdown.
- **FR-006**: Do not infer DI container activation, reflection, runtime factories, or native-to-managed constructor paths.
- **FR-007**: Keep semantic analysis opt-in and preserve static scan behavior and existing method call output.
- **FR-008**: Add C#/VB compilation fixtures and call-impact tests; add no new NuGet dependencies.

## Success Criteria

- **SC-001**: Every supported, compiler-resolved constructor edge in the frozen C#/VB fixtures is emitted once with the exact overload and source location; unresolved constructors are never labeled resolved.
- **SC-002**: A changed explicit constructor yields every expected in-repository direct and transitive constructor caller, while method call findings and counts remain unchanged.
- **SC-003**: Constructor syntax count equals resolved plus unresolved constructor counts, with sample omissions and project coverage reflected in completeness.
- **SC-004**: Output is deterministic and includes an explicit `constructor` relationship kind; default non-semantic reports contain no resolved constructor edges.

## Assumptions

- Roslyn can bind the selected construction syntax in loaded projects without building legacy projects; semantic mode remains opt-in because MSBuild project evaluation can run imported targets.
- An explicit constructor call is a source-level construction relationship, not proof that the code executes at runtime.
- Container-driven activation and reflection require separate evidence and remain outside this feature.
