# Research: Filter Value-Returning Factory Candidates

## Decision: Use declared return types to exclude string helpers

The C# analyzer parses `MethodDeclarationSyntax`; the VB.NET analyzer parses `MethodStatementSyntax`. Both currently treat any non-void method with a `Create`, `Build`, or `Resolve` prefix as a factory candidate. BrownPants' own report confirms this rule flags `ResolveCustomActiveWorkItemPath`, whose declared return type is nullable string.

Narrow the existing syntax rule for string return types in both languages. Keep object-returning methods and existing factory-type, factory-invocation, and DI signals unchanged. Candidate evidence remains heuristic and source-located.

**Rationale**: This removes the reproduced false positive without project loading, symbol resolution, or cross-language runtime claims.

**Alternatives considered**:
- Remove `Resolve*` methods: rejected because resolvers can construct or resolve objects.
- Exclude method names ending in `Path`: rejected because it special-cases one symbol rather than using source evidence.
- Load project semantics: rejected because basic classification would depend on project configuration and references.

## Repository evidence

- `ArchitecturePatternAnalyzer.IsFactoryMethod(MethodDeclarationSyntax)` excludes `void` but not string.
- The VB.NET overload accepts any `Function` with a factory-like name.
- Self-inspection identified `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs:631`.
- Existing tests assert C# and VB.NET object-returning `CreateWidget` methods remain visible.