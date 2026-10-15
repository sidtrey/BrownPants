# Native Flow Evidence Contract

Repository inventory JSON exposes an additive `nativeFlow` analysis object. Each relation includes `id`, `kind`, `status`, `sourcePath`, `sourceLine`, `sourceSymbol`, `targetPath`, `targetLine`, `targetSymbol`, `commandId`, `candidateTargetCount`, and `evidence`. Paths are repository-relative and lines are one-based. Null target locations indicate unresolved or ambiguous relations.

The engine-neutral evidence command includes unique links under category `native-flow-candidate`, with status `candidate`, the source path/line, target path/line fields, symbol identities, and evidence. Unresolved and ambiguous links remain explicit in the native-flow summary and are not emitted as successful edges. No native relation is labeled compiler-resolved or merged into the C#/VB semantic call graph.

Markdown inspection lists source-to-target candidate paths and separately summarizes unresolved/ambiguous relations and sampling. Existing schema version remains unchanged because all fields are additive.
