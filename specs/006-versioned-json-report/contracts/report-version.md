# Report Version Contract

Successful `inspect --format json` output is one repository inventory object. Its first field is:

```json
{
  "SchemaVersion": 1
}
```

The full object retains the fields documented in [the project inventory contract](../../002-cross-ecosystem-inventory/contracts/project-inventory.md). Version 1 is independent of the CLI/package version. Optional additive fields remain version 1; removing or renaming a field, changing its type, or changing its documented meaning requires a new integer schema version.

## Consumer behavior

Consumers SHOULD check the root `SchemaVersion` before interpreting other fields. Version 1 identifies the current JSON field names, types, and meanings. Package version and report schema version are independent.

## Evolution

Optional additive fields remain version 1. A future breaking change that removes or renames an existing field, changes its type, or changes its documented meaning MUST use a new integer schema version. Markdown output has no schema version marker.

