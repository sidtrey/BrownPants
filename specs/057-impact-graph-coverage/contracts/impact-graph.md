# Impact Graph Contract

- Public semantic `ResolvedCalls` remains sampled by the requested display limit.
- `impact` traverses the bounded internal graph when semantic analysis supplies it; callers omitted only from the public sample can still be returned.
- The internal graph is not serialized in `inspect` output.
- If the internal graph reaches its fixed bound, impact reports retained and omitted resolved edges and sets `IsComplete` to false.
- If the graph is complete, completeness still accounts for unresolved relationships, declaration sampling, failed project loads, and workspace issues.
- Analyses constructed without the internal graph retain the existing behavior of traversing their public `ResolvedCalls` list.
