# Research: Source-Backed Constructor Declarations

## Decisions

- Use the existing Roslyn `INamedTypeSymbol.InstanceConstructors` collection while walking C# and VB type declarations. It is the compiler's authoritative set of constructors for a source type and includes generated members when the compiler exposes them.
- Keep the current constructor signature formatter. A resolved `IMethodSymbol` constructor target and its declaration entry must serialize to exactly the same symbol string for impact graph joins.
- Resolve declaration anchors from constructor syntax references or source locations. Use the containing type declaration only as the source anchor for an implicit constructor with no constructor-specific syntax; omit metadata-only constructors.
- Preserve explicit constructor syntax collection and deduplicate before producing counts or bounded declaration samples.
- Add fixture-based tests for a class with an implicit default constructor, a C# primary constructor, a record case if Roslyn exposes a stable source constructor, explicit overload compatibility, and VB source-backed implicit construction.

## Rationale

The semantic analyzer already binds object creation to constructor symbols, but changed-file impact requires a declaration symbol in the same graph. Enumerating constructors from their source type provides a source-backed declaration anchor without changing constructor edge semantics or making project evaluation more permissive.

## Alternatives Considered

- Infer constructor targets from type names alone: rejected because it can merge overloads and contradicts compiler-resolved evidence.
- Add implicit constructor declarations in the impact analyzer: rejected because declaration discovery belongs in the Roslyn semantic layer, where symbol identity and source locations are available.
- Fabricate a type location for metadata constructors: rejected because metadata targets are outside the repository and cannot seed source impact.
