# Data Model: Custom SDD Active Work Item

## Custom Workflow Definition

The existing declarative workflow definition adds one optional property:

| Field | Type | Meaning |
|---|---|---|
| `activeWorkItemPath` | optional string | Explicit repository-relative directory selected by the maintainer. It must be below a declared artifact root and represented by scanned files. |

The path is normalized to `/` separators for report output. Invalid, out-of-root, or missing selections add a configuration issue and do not invalidate other workflow inventory.

## SDD Workflow Inventory

The existing nullable `ActiveWorkItemPath` and `ActiveWorkItemEvidence` fields are populated with the normalized custom path and `CustomWorkflowDeclaration` only after validation. Otherwise both remain null.

## Configuration Issues

Issues use `.brownpants/sdd-workflows.json` as their path. `InvalidActiveWorkItemPath`, `ActiveWorkItemOutsideArtifactRoots`, and `ActiveWorkItemNotFound` messages do not echo configured values.
