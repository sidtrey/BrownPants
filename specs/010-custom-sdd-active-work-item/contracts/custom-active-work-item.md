# Custom Active Work Item Configuration Contract

Add `activeWorkItemPath` to a workflow object in `.brownpants/sdd-workflows.json`:

```json
{
  "schemaVersion": 1,
  "workflows": [
    {
      "name": "ExampleSDD",
      "artifactRoots": ["planning"],
      "specificationRoots": ["planning/specs"],
      "changeRoots": ["planning/changes"],
      "archivedChangeDirectoryNames": ["archive"],
      "activeWorkItemPath": "planning/changes/add-search"
    }
  ]
}
```

The property is optional. Its value must be a safe repository-relative directory below one of the workflow's `artifactRoots` and must contain at least one scanned file. Either slash style is accepted and reports use `/`. Invalid or stale selections generate `SddWorkflowIssues` and leave `ActiveWorkItemPath` and `ActiveWorkItemEvidence` null while retaining the workflow's other inventory. Valid selections use evidence `CustomWorkflowDeclaration`. No target file is read and the report schema remains version 1.
