# Research: Local Delegate Impact

## Decisions

- Use Roslyn semantic symbols to select a method-group overload and the anonymous-function symbol for lambda/anonymous-method initializers. `IMethodReferenceOperation` represents source method references, and `IAnonymousFunctionOperation` represents anonymous function bodies; these are compiler-bound evidence, not runtime execution. See Microsoft's [Operations namespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations?view=roslyn-dotnet-5.3.0) and [IMethodReferenceOperation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.imethodreferenceoperation?view=roslyn-dotnet-5.0.0).
- Resolve only a local with one initializer and invocation-only local uses in the same callable. Reassignment, capture, escape, field/parameter storage, or ambiguous use produces unresolved delegate evidence instead of a guessed target. General points-to or control-flow-sensitive analysis is out of scope.
- Add delegate relationships to the existing `CSharpResolvedCall` graph with `RelationshipKind = "delegate"`. Preserve compiler-resolved `Delegate.Invoke` invocation counts and existing method/constructor counts.
- Keep delegate unresolved totals/samples separate from ordinary invocations and constructors. Include unresolved targets in impact completeness so users know missing edges may affect impact output.
- Additive report fields remain schema version 1 because existing JSON properties and invocation meanings are preserved. Validate both CLI evidence and impact reports.
- Use no new package. Roslyn is already the compiler front end; the [ControlFlowGraph API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.flowanalysis.controlflowgraph?view=roslyn-dotnet-5.3.0) supports lambda/local-function graphs but full flow analysis is unnecessary for the intentionally conservative rule.

## Alternatives Considered

- Treat every local delegate invocation as its initializer's target: rejected because reassignment, capture, or escape makes the target ambiguous.
- Add all delegate relationships to resolved invocation counts: rejected because this duplicates syntax counts and changes existing metric meanings.
- Infer delegate targets from names or textual assignments: rejected because it bypasses compiler overload selection and can connect unrelated symbols.
- Add a new analysis dependency or an interprocedural points-to engine: rejected because it adds package/trust surface and exceeds the local-flow scope.

## Comparison Design

The approved acceptance criterion requires three isolated matched pairs, same frozen source and prompt, same Codex model/CLI/sandbox/schema, counterbalanced execution order, and source-derived scoring with the key outside agent workspaces. The threshold is a pooled 20 percentage-point gain in actionable managed-impact recall with no precision loss and at least two positive pairs, plus passing independent regression checks in both conditions. See [acceptance-criteria-review.md](acceptance-criteria-review.md). A single feature test or token reduction is not proof of quality lift.
