# Repeated Candidate Groups Contract

Evidence JSON adds `repeatedCandidateGroups` to the version-1 report. Each item has:

- `category`, `signal`, and `path` copied exactly from matching findings.
- `locationCount`, at least 2.
- `locations`, containing sorted `{ findingId, line }` links to emitted entries in the existing `findings` array.

Only `architecture-candidate` and `framework-candidate` items with a source line are eligible. The exact category/signal/path tuple is the group key. Existing `findings`, their IDs, counts, `focus`, completeness fields, and SchemaVersion remain unchanged. Focused reports rebuild groups from their selected findings, so no group points to an omitted finding. Group data only summarizes the bounded emitted sample; consult the existing completeness fields when findings were truncated.
