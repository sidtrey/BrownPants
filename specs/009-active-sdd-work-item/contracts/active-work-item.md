# Active Work Item Report Contract

Each object in `SddWorkflows` adds two nullable properties:

```json
{
  "ActiveWorkItemPath": "specs/009-example",
  "ActiveWorkItemEvidence": "SpecKitFeaturePointer"
}
```

`ActiveWorkItemPath` is a normalized repository-relative directory with `/` separators. `ActiveWorkItemEvidence` is `SpecKitFeaturePointer` or `UniqueOpenSpecChange`. When no unambiguous, validated selection exists, both properties are JSON null. Existing report schema version remains `1`; all prior properties retain their names and meanings.

Markdown includes a `- Active work item:` line with the path and evidence label only when a selection exists. Invalid Spec Kit selection metadata appears under the existing configuration diagnostics section.
