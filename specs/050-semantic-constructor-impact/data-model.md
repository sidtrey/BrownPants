# Data Model: Semantic Constructor Impact

- **Relationship kind**: `method` for invocation expressions; `constructor` for object creation and constructor initializers.
- **Resolved edge**: project, source path/line, expression, caller symbol, constructed target signature, canonical target identity, optional target path/line, and relationship kind. Canonical identity uses the original Roslyn definition so closed generic calls join to declarations.
- **Unresolved edge**: project, source path/line, expression, Roslyn candidate reason/targets, and relationship kind.
- **Method counts**: existing invocation syntax/resolved/unresolved counters retain their current meanings.
- **Constructor counts**: separate syntax/resolved/unresolved counters reconcile independently.
- **Callable declarations**: method, local function, and explicit constructor declarations serve as impact graph targets.
- **Impact finding**: changed symbol, caller/target, call-site path/line, depth, project, test-path candidate flag, and relationship kind.

Resolved edge arrays remain bounded by the existing sample limit; total counters and completeness warnings expose omitted findings.
