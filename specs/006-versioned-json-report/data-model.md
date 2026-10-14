# Data Model: Versioned JSON Report

## JSON report root

The existing repository inventory object gains one field:

- **SchemaVersion**: integer `1` identifying the compatibility contract of all remaining root and nested fields.

The value is constant across repository roots, detected ecosystems, semantic mode, and SDD workflow configuration. It is independent of package version and file contents.

## Compatibility rule

- Additive optional fields preserve schema version 1.
- Removing or renaming existing fields, changing a field type, or changing a documented field meaning increments the schema version.
- Markdown reports do not include this JSON-only field.

