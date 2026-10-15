# Data Model: Native Call Impact

## Impact Finding

Represents a source-located relationship relevant to a changed path.

- `changedPath`: validated repository-relative changed source path.
- `evidenceKind`: `managed-compiler-resolved` or `native-lexical`; native `status` separately records candidate, unresolved, or ambiguous state.
- `relationshipKind`: invocation, constructor, MFC dispatch, or qualified native call.
- `sourceSymbol` / `targetSymbol`: caller and callee identities where available.
- `sourcePath` / `sourceLine`: relationship site.
- `targetPath` / `targetLine`: uniquely identified target definition; absent when unresolved or ambiguous.
- `depth`: observed reverse-trace distance; candidate evidence is not runtime behavior.
- `status`: resolved managed relationship or candidate/unresolved/ambiguous native relation.

## Completeness

The report is complete only when path validation, semantic loading, native relation sampling, and requested graph traversal report no known gaps. Unresolved and ambiguous native relations, omitted native relations, sampled findings, workspace issues, and unresolved managed relationships remain explicit limitations. Completeness does not assert runtime impact or graph exhaustiveness.
