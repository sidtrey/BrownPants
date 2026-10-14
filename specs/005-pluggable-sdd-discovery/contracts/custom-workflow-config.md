# Custom SDD Workflow Configuration Contract

Configuration is optional and located at `.brownpants/sdd-workflows.json`.

```json
{
  "schemaVersion": 1,
  "workflows": [
    {
      "name": "ExampleSDD",
      "artifactRoots": ["planning"],
      "specificationRoots": ["planning/specs"],
      "changeRoots": ["planning/changes"],
      "archivedChangeDirectoryNames": ["archive", "completed"],
      "activeWorkItemPath": "planning/changes/add-search"
    }
  ]
}
```

## Rules

- Property names are case-sensitive camelCase. Unknown properties are invalid.
- `schemaVersion` is required and must equal `1`; `workflows` is a non-empty array of at most 100 definitions.
- `name` is required, non-empty, unique without regard to case, and cannot equal Spec Kit or OpenSpec.
- `artifactRoots` is a required non-empty array. The other root arrays and archive names are optional.
- Root values use forward slashes, are relative directory paths, and contain no empty, `.`, or `..` segment, wildcard, drive/URI prefix, or trailing slash.
- `activeWorkItemPath` is optional. It must be a safe repository-relative directory below one of that workflow's `artifactRoots`, and at least one scanned file must be beneath it. Both slash styles are accepted and report paths use `/`.
- Archive values are non-empty single directory names, without slash, backslash, or wildcard.
- A definition with no files under its artifact roots is reported as not initialized.
- An invalid or stale active selection adds a diagnostic without invalidating the workflow's other inventory. A valid selection sets `ActiveWorkItemPath` and `ActiveWorkItemEvidence: CustomWorkflowDeclaration`.
- Configuration is descriptive only. BrownPants does not execute commands or load plugins named by the file.
- The declaration is limited to 1 MiB. Invalid JSON, unsupported schema versions, and invalid definitions produce structured SDD diagnostics while built-in detection continues.

## JSON report additions

Add `SddWorkflowIssues`, an array of `{ Path, Code, Message }`. The existing `SddWorkflows` array includes custom rows; built-in rows retain their existing values.

## Markdown report additions

The SDD workflows section includes custom workflow rows and a diagnostics subsection when issues exist. Valid active paths appear with their evidence label. All displayed paths remain repository-relative; messages contain no absolute host paths.
