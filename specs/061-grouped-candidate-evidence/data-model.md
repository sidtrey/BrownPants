# Data Model: Grouped Candidate Evidence

## Repeated candidate group

- `Category`: `architecture-candidate` or `framework-candidate`.
- `Signal`: the exact signal label from the original findings.
- `Path`: normalized repository-relative source path.
- `LocationCount`: number of emitted source-located findings in the group; at least two.
- `Locations`: sorted `{ FindingId, Line }` references to the canonical per-line findings.

Identity is the exact `(Category, Signal, Path)` tuple. Groups are sorted by category, path, then signal using ordinal ordering. Locations are sorted by line, then finding ID. Groups are derived only from findings retained under the existing sample limit. If raw findings were truncated, the report's existing completeness field remains authoritative and a group represents sampled locations only.

Managed calls, native-flow relations, and test-path candidates are excluded. A repeated candidate group does not establish that locations have the same behavior or bug relevance.
