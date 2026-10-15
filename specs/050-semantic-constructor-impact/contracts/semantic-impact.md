# Semantic Edge and Impact Contract

Existing method edges retain their current identity and `RelationshipKind: method`. Constructor edges use `RelationshipKind: constructor` and the Roslyn constructor symbol signature, including declaring type and parameter types. Every target is labeled resolved only when Roslyn returns an `IMethodSymbol` with `MethodKind.Constructor`; unresolved edges retain `CandidateReason` and candidate symbols.

Method invocation counters remain unchanged. Constructor syntax, resolved, and unresolved counters reconcile separately. Resolved/unresolved edge arrays are bounded by `--limit`; reports state total counts and omissions. Target path/line is present only for a source location inside the repository.

Impact JSON adds `relationshipKind` to each finding. Markdown identifies constructor callers explicitly. Static inventory does not emit resolved constructor relationships. Semantic project loading remains opt-in; no edge asserts runtime execution, container activation, reflection, or native calls.
