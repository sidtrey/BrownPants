# Data Model: Active SDD Work Item

## SDD Workflow Inventory

Existing entity returned once per detected workflow.

| Field | Type | Meaning |
|---|---|---|
| `ActiveWorkItemPath` | nullable string | Repository-relative directory for a selected active feature or change; null when absent, invalid, stale, or ambiguous. |
| `ActiveWorkItemEvidence` | nullable string | Stable label identifying the selection source; null whenever `ActiveWorkItemPath` is null. |

Allowed evidence labels:

- `SpecKitFeaturePointer` — validated `.specify/feature.json` target represented in scanned files.
- `UniqueOpenSpecChange` — the only non-archived first-level OpenSpec change directory represented in scanned files.

Custom SDD workflows keep both values null in this feature.

## SDD Workflow Configuration Issue

The existing issue entity also reports invalid active Spec Kit pointer state with path `.specify/feature.json` and stable codes such as `InvalidActiveFeatureJson`, `UnsafeActiveFeaturePath`, `ActiveFeatureNotFound`, and `DuplicateActiveFeatureFile`. Messages must not include untrusted raw path values.
