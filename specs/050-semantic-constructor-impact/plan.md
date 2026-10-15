# Implementation Plan: Semantic Constructor Impact

## Summary

Extend the existing Roslyn semantic graph with explicit constructor declarations and compiler-bound object creation/chaining in C# and Visual Basic. Preserve current method invocation counters, add separate constructor syntax/resolved/unresolved counters, label each edge's relationship kind, and let the existing impact traversal follow constructor edges. No project build or new package is required by default.

## Technical Context

- CLI and analyzer target .NET 10 with nullable enabled.
- Roslyn is already used for C# and VB syntax and semantic binding.
- Semantic MSBuild loading remains explicitly opt-in.
- xUnit fixture tests compile temporary C# and VB syntax trees; impact tests use synthetic compilations.
- No new NuGet dependencies.

## Repository Evidence

- `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs` currently walks only `InvocationExpressionSyntax` and records resolved `IMethodSymbol` targets. This excludes object creations and constructor initializer syntax.
- `CSharpSemanticDeclaration` currently includes methods/local functions but not constructors, so constructor targets cannot be traversal roots.
- `CallImpactAnalyzer` traverses `CSharpResolvedCall` edges by exact caller/target symbol strings. It can traverse constructors once declarations and edges share the constructor symbol identity.
- `CallImpactReportRenderer` and `MarkdownReport.AppendSemanticAnalysis` display invocation counts and edge samples; they must distinguish method invocations from constructor calls after edges are combined.
- `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` verifies overload resolution, exact in-repository target paths, cycles, deduplication, and transitive impact.
- `tests/BrownPants.Tests/RepositoryScannerTests.cs` verifies C#/VB semantic resolution and completeness accounting.

## Constitution Check

- Evidence First: compiler-bound constructor symbols are resolved edges; container/reflection/native activation remains unsupported or unresolved.
- Broad, Engine-Neutral Core: the semantic JSON and impact report stay workflow neutral.
- Deterministic and Reviewable: preserve exact signatures, source locations, and bounded samples.
- Safe Inspection: semantic mode remains opt-in and documents MSBuild evaluation.
- Specified and Verified Changes: this feature uses the pinned acceptance hash and evidence ledger.

## Design

### Semantic records

Add `RelationshipKind` to resolved and unresolved edge records with stable values `method` and `constructor`. Add independent constructor syntax, resolved, and unresolved counts to `CSharpSemanticAnalysis`; keep existing invocation counts invocation-only. Keep `ResolvedCalls`/`UnresolvedCalls` as bounded combined edge lists. Include explicit constructor declarations in the declaration graph.

### Supported syntax

- C#: explicit `new T(...)`, target-typed `new(...)`, and `this(...)`/`base(...)` constructor initializers.
- Visual Basic: `New T(...)` object creation where Roslyn binds an `IMethodSymbol` with `MethodKind.Constructor`.
- Ignore array creation, delegate construction, and implicit runtime/container activation unless Roslyn reports an explicit constructor edge covered above.

### Impact and output

The impact graph matches compiler-bound targets to declarations using canonical original-definition identity. This lets a call through `OrderImportWorkflow<Order>` match its open generic declaration `OrderImportWorkflow<T>` while retaining the constructed signature in the edge. Add a relationship-kind field to impact JSON and readable labels to Markdown. Keep existing method invocation totals unchanged; show separate constructor totals, unresolved counts, and sample omissions. Ensure `evidence` distinguishes resolved constructors in its finding signal and counts constructor completeness.

## Risks and Mitigations

- Method counts could drift if constructor syntax is mixed into invocation counters: maintain separate counters and reconciliation assertions.
- Multiple overloads share a type/name: use Roslyn method symbols and full parameter signatures.
- Implicit constructors may have type-level source locations: identify them as constructor edges, but report a target line only when Roslyn provides an in-repository source location.
- Semantic evidence does not prove runtime execution or DI container activation: retain explicit limitations.

## Validation

Build and test the solution. Add C# and VB semantic fixtures for overloaded constructors, target-typed `new`, constructor chaining, unresolved construction, and method calls. Verify exact signatures/locations, independent count reconciliation, deterministic ordering, and constructor-based direct/transitive impact. Run BrownPants semantic inspection on this repository and the bug-lab fixture; confirm default static output contains no resolved constructor edges.
