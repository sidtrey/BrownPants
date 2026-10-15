# Data Model: Managed Interop Impact

## Managed Method Target

- `language`, `namespace`, `typeName`, `methodName`, `path`, `line`
- Internal bounded syntax index used for source matching; not a compiler symbol.

## Native Flow Candidate

- `kind`: includes `cppcli-managed-call` for a bridge-to-managed source candidate.
- `status`: `candidate`, `unresolved`, or `ambiguous`.
- `sourceSymbol`, `sourcePath`, `sourceLine`: bridge method and call site.
- `targetSymbol`: source-level managed namespace/type/method identity.
- `targetPath`, `targetLine`: populated only for one unique declaration.
- `candidateTargets`: bounded set of possible declaration locations; empty when none.
- `candidateTargetCount`: total number of matching declarations before output sampling.

## Native Impact

An impact entry associates a changed native or managed path with one native-flow relation, its evidence kind and status, source/target identities and locations, candidate target locations, and reverse-trace depth. Managed semantic impact remains a separate collection.
