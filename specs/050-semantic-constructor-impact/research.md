# Research: Semantic Constructor Impact

## Existing behavior

The semantic pass walks C#/VB invocation-expression syntax, binds each node with Roslyn, and stores resolved method edges in `CSharpResolvedCall`. Method and local-function declarations form the target graph. `CallImpactAnalyzer` traverses this graph to report direct and transitive callers.

## Evidence of the brownfield gap

Factories and dependency-injection registration are detected by `ArchitecturePatternAnalyzer` as lexical/source candidates, but the semantic graph does not include object construction or explicit constructor chaining. Therefore a changed constructor has no resolved caller path through `new Service(...)`, target-typed `new()`, or VB `New Service(...)` sites. Existing reports distinguish this limit for other constructor/object-creation relationships.

## Decision

Treat explicit compiler-bound constructor relationships as semantic edges with a separate relationship kind and counters. This is source-level binding evidence, not runtime execution evidence. Keep implicit container activation, reflection, native activation, and unresolved constructions out of confirmed edges. Do not add packages or evaluate projects in static mode.

## Open questions

None; unsupported activation mechanisms remain explicit limitations.
