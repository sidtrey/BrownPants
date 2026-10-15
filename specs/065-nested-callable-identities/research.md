# Research: Nested Callable Identities

## Decision: Format compiler callable scopes recursively

`CSharpSemanticAnalyzer` obtains call origins from `SemanticModel.GetEnclosingSymbol`. Roslyn returns `IMethodSymbol` values for anonymous and local functions, but the current formatter emits only containing type, method name, and parameters. An anonymous function has no user-defined method name, which produced `OrderBatchFactory.(WorkScope)` in the Bug Lab DI registration lambda.

Use `MethodKind` to distinguish anonymous and local functions. Source references provide a deterministic anchor for anonymous callables. Include repository-relative path, line, and column, with a fixed redacted marker for external source. Local function names are user-authored, but require their parent identity to distinguish same-named functions in different methods.

**Alternatives considered**: Keep the generated/empty name (ambiguous); use a compiler-generated metadata name (unstable and unreadable); attribute all lambda calls to the containing named method alone (merges separate callbacks); or use absolute syntax paths (machine-specific and private).

## Decision: Share one formatter across declarations, targets, and callers

Call impact keys graph edges by caller and target identity. Formatting only the display caller would leave local-function declarations and call targets mismatched. The formatter therefore applies uniformly to semantic declarations, resolved call targets, and enclosing callable contexts. Ordinary named methods and constructors retain existing output.

## Decision: Validate C# and VB through Roslyn compilations

The shared analyzer already supports C# and Visual Basic compilations. Existing semantic fixture conventions in `RepositoryScannerTests.cs` construct compiler inputs without project builds; extend those patterns to cover nested callable identities and impact matching.
