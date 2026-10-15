# Data Model: Local SDD Review Workbench

## Workbench document

- `FeaturePath`: canonical repository-relative feature directory.
- `Artifacts`: ordered set of artifacts found beneath the feature directory; each has a stable `kind`, relative path, source text, and optional parsed status summary.
- `Stories`: stable story identifier and heading text with associated acceptance criteria.
- `Criteria`: stable AC/SC identifier, source text, and source artifact path.
- `Tasks`: task ID, description, source path, and checked/unchecked state.
- `EvidenceEntries`: task ID, criterion IDs, verification command, evidence paths, and recorded result from `acceptance-evidence.json`.

## Review note export

The exported JSON contains a schema version, feature-relative path, and ordered notes. Each note has a stable local item reference (`kind` and `id` or artifact path) and free-text content. Notes do not change the workbench input or output.

Task checkboxes and evidence ledger results are reported as recorded values. The UI does not treat either one alone as proof that behavior was tested or accepted.
