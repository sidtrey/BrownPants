# Data Model: On-Demand Instruction Sections

## Instruction Section

A read-only result for one selected section in one supported instruction source.

| Field | Type | Rules |
|---|---|---|
| SchemaVersion | Integer | Stable version marker for JSON consumers; value is `1`. |
| SourcePath | String | Repository-relative path using `/` separators; source must be recognized or explicitly selected and pass repository safety checks. |
| Heading | String | Original heading text, without Markdown prefix or closing hashes. |
| Anchor | String | Same normalized unique anchor exposed by the instruction inventory. |
| StartLine | Integer | 1-based line number of the selected heading. |
| EndLine | Integer | 1-based inclusive final line before the next heading of equal or higher level, or end of file. |
| Content | String | Selected heading and nested section text, normalized to LF; no adjacent peer heading/body. |

The source digest remains an inventory identity and is not a substitute for reviewing the returned content. No entity is persisted.
