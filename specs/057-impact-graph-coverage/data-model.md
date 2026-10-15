# Data Model: Impact Graph Coverage

## Public semantic report sample

- Existing `ResolvedCalls` collection remains bounded by the user-selected `--limit`.
- Existing resolved invocation and constructor total counts remain unchanged.
- The internal graph is omitted from JSON serialization and does not change the public schema.

## Internal impact graph

- Holds resolved invocation and constructor relationships in deterministic semantic traversal order.
- Has a fixed maximum retained edge count of 100,000 in production.
- Tracks the exact number of resolved edges omitted after reaching that ceiling.
- Is preferred by `CallImpactAnalyzer`; manually constructed semantic analyses without an internal graph fall back to `ResolvedCalls`.

## Completeness

- Display-sample omission does not by itself make impact incomplete when the internal graph retains all resolved relationships.
- Internal graph omission marks impact incomplete and gives retained/total counts.
- Unresolved relationships, declaration sampling, incomplete project loads, and workspace issues remain separate reasons.
