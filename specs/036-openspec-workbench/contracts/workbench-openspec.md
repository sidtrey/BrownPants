# OpenSpec Workbench Contract

## Command

```text
brownpants workbench <repository> --engine openspec [--feature openspec/changes/<change-name>] [--output <relative-html-path>] [--overwrite]
```

- OpenSpec is explicit; without `--engine`, the existing Spec Kit pointer behavior is preserved.
- Without `--feature`, one immediate non-archived directory under `openspec/changes/` must exist. Multiple or zero changes produce an actionable error.
- Explicit selection must name one non-archived direct child of `openspec/changes/`.
- `proposal.md` is required. Root `design.md`, `tasks.md`, and nested `specs/**/spec.md` files are optional.
- Requirement and scenario references use the repository-relative delta spec path plus a stable per-file heading ordinal, for example `openspec/changes/add-checkout/specs/payments/spec.md#requirement-001`.
- The report retains original artifact text and identifies source paths. Checklist state is reported as recorded, not as proof of verification.
- Each artifact is limited to 2 MiB, the combined content to 8 MiB, artifact count to 1000, nested delta directories to 1000, and traversed delta entries to 10000. Inputs must be valid UTF-8. Symbolic links/reparse points are refused; all paths stay in the repository.
- Existing output is protected unless `--overwrite` is supplied, and selected source artifacts remain protected even when overwrite is explicit.
- Review notes use the existing version 1 JSON handoff and are downloaded locally by the browser; no source file or service is modified.
