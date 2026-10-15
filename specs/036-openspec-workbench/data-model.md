# Data Model: OpenSpec Review Workbench

The existing workbench report adds an engine label and proposal summary while retaining its shared artifact, review-point, and task records.

- `Engine`: `Spec Kit` or `OpenSpec`; controls user-facing section labels and selection behavior.
- `FeaturePath`: repository-relative Spec Kit feature or OpenSpec change directory.
- `ProposalSummary`: extracted OpenSpec `Why` and `What Changes` sections; original proposal remains available.
- `ReviewPoints`: requirement/scenario heading text, kind, and source path. OpenSpec note IDs combine the delta spec path with a per-file heading ordinal (for example `.../spec.md#scenario-001`).
- `Tasks`: recorded completion state and repository-relative `tasks.md` source path. OpenSpec task IDs use their authored number where present or a deterministic file-order ID.
- `Artifacts`: repository-relative paths and original UTF-8 text, rendered as escaped display text.

OpenSpec does not provide the Spec Kit acceptance evidence ledger or SHA-256 criteria baseline, so the OpenSpec report marks the Spec Kit baseline as not applicable and does not show an empty evidence-ledger section.
