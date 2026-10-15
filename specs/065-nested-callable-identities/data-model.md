# Data Model: Nested Callable Identities

## Semantic callable identity

A stable string describing the source callable that owns a resolved call or declaration.

- **Named method**: Existing fully qualified containing type, name, generic arguments, and parameter types.
- **Local function**: Parent callable identity plus the local function's signature.
- **Anonymous function**: Parent callable identity plus a lambda/anonymous marker anchored by normalized repository-relative path and one-based line/column; external source uses a redacted fixed marker plus line/column.
- **Call edge**: Existing caller, target, source and target locations, relationship kind, and evidence classification; the corrected identity keeps callers and local targets joinable for impact analysis.

Identities are deterministic for unchanged source and do not assert that callbacks execute.
