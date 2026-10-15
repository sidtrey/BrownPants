# Managed Interop Evidence Contract

The `inspect`, `evidence`, and `impact` reports may include `cppcli-managed-call` relations derived from a declared C++/CLI managed handle and an instance call through that handle.

- A unique source declaration is labeled `candidate`; multiple declarations are `ambiguous`; no declarations are `unresolved`.
- Every relation identifies the call site and target symbol. Candidate declaration paths and lines are retained for ambiguous matches.
- Native impact from a changed managed path may include these candidates and any supported upstream qualified-native or MFC-dispatch relations.
- Native relations remain separate from Roslyn-resolved managed calls in JSON and Markdown.
- The analyzer does not prove project references, compiler binding, generic instantiation, runtime activation, or execution.
