# Feature Specification: Source-Backed Constructor Declarations

**Feature Branch**: `066-source-backed-constructors`

**Created**: 2026-10-15

**Status**: Draft

**Input**: Extend brownfield impact tracing to source-backed constructors that Roslyn resolves but that are not declared with explicit constructor syntax, including implicit parameterless constructors and C# primary constructors.

## User Scenarios & Testing

### User Story 1 - Trace construction into implicit constructors (Priority: P1)

A maintainer changes a class whose parameterless constructor is compiler-generated and needs construction callers from the same semantic graph.

**Why this priority**: Current impact seeds come from scanned declaration syntax. Compiler-resolved construction edges can target implicit constructors that have no constructor declaration node, so a changed type can appear to have no mapped declarations.

**Independent Test**: Compile a class without an explicit constructor plus a caller that constructs it; seed impact from the class source and verify the caller is returned as a constructor relationship.

**Acceptance Scenarios**:

1. **AC-001** Given a C# class with an implicit parameterless constructor, when semantic analysis resolves its object creation, then the constructor target has a matching declaration anchored to the source type.
2. **AC-002** Given that class file as changed, when impact runs, then the construction caller is reported with its path, line, depth, and `constructor` relationship kind.

### User Story 2 - Trace C# primary constructors (Priority: P2)

A maintainer reviews modern C# code using a primary constructor and needs changes to its type declaration to reveal compiler-bound construction callers.

**Independent Test**: Compile a primary-constructor class and one or more call sites; verify the target identity matches a declaration on the primary-constructor source line and impact reaches each caller.

**Acceptance Scenarios**:

1. **AC-003** Given a C# class or record with a primary constructor, when semantic analysis resolves construction, then the primary constructor is represented once at its source declaration and its callers are traceable from the type file.
2. **AC-004** Given explicit overloaded constructors and existing VB semantic fixtures, when analysis runs, then their identities, counts, and source locations remain unchanged; source-backed implicit VB constructors are included when Roslyn exposes them.

### Edge Cases

- Implicit constructors may have a Roslyn source location on the containing type even when they have no declaration syntax reference.
- Records may expose compiler-generated constructors; emit each supported source-backed constructor identity once.
- Metadata-only constructors remain without repository declaration locations.
- A type with explicit constructors must not gain a duplicate implicit default-constructor declaration.

## Requirements

### Functional Requirements

- **FR-001**: Semantic declaration mapping MUST include source-backed implicit constructors and C# primary constructors that Roslyn binds to constructor relationships.
- **FR-002**: Constructor call targets and their source declarations MUST use the same canonical identity and in-repository source path.
- **FR-003**: Changed source files containing implicit or primary constructor declarations MUST seed impact traversal and report compiler-bound construction callers with relationship kind `constructor`.
- **FR-004**: Explicit constructor overloads, constructor counts, method identities, deterministic ordering, and existing C#/VB output MUST remain compatible.
- **FR-005**: Generated or metadata symbols without a usable repository source location MUST NOT be assigned a fabricated path or exposed external absolute path.
- **FR-006**: Reports MUST describe compiler-bound source relationships and MUST NOT imply that object creation executes at runtime.

## Success Criteria

### Measurable Outcomes

- **SC-001**: C# fixtures for implicit default and primary constructors each produce one source-backed declaration matching the resolved constructor target.
- **SC-002**: Impact seeded from each constructor source file returns all fixture construction callers with correct relationship kinds and locations.
- **SC-003**: Explicit overload, C#/VB semantic, determinism, and constructor count regression tests continue to pass.
- **SC-004**: Full Release build and test suite pass; BrownPants-on-BrownPants semantic coverage and existing Bug Lab impact remain stable.

## Assumptions

- Roslyn's source locations for implicit constructors are suitable type-level anchors when they point into the scanned repository.
- C# primary constructors are in scope because the CLI targets .NET 10 and Roslyn already provides their symbols.
- Runtime activation, dependency injection, reflection, and native calls remain outside compiler-bound constructor impact unless explicitly resolved by existing supported analysis.
