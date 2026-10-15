# WinForms Analysis Report Contract

The `winforms analyze <repository> [--format markdown|json] [--limit 1..10000]` command remains source-only and keeps the existing finding fields and deterministic order.

Additive JSON coverage fields (the existing serializer uses PascalCase):

- `UnresolvedReceiverCount`: number of simple property assignments whose receiver could not be tied to a declared type under supported syntax rules.
- `IndexedTypeCount`: source type identities indexed across all parsed C#/VB files.
- `Issues`: bounded source locations and stable reason codes, including `BPWF100` unresolved receiver records; no source text excerpts.

Findings remain `Candidate`; no field may claim a write is safe or a confirmed runtime defect. `unresolvedReceiverCount` is a syntax coverage measure, not a count of missing WinForms defects. Its source locations use the existing bounded `issues` array and reason ID `BPWF100`. Markdown must explain that source-only type resolution is approximate and project conditions are not evaluated.
