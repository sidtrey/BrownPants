# Workbench CLI and Feedback Contract

## Command

```text
brownpants workbench <repository> [--feature <relative-feature-directory>] [--output <relative-html-path>] [--overwrite]
```

- Default feature: repository-relative `feature_directory` from `.specify/feature.json`.
- Default output: `TestResults/brownpants-workbench.html` beneath the repository.
- `spec.md` is required. `plan.md`, `tasks.md`, `acceptance-evidence.json`, and `verification.md` are optional.
- All reads and writes stay under the repository root. Existing output is protected unless `--overwrite` is supplied.
- Missing/malformed/escaping feature pointers and missing `spec.md` fail with a nonzero code and no output.

## Review notes JSON

Schema version 1:

```json
{
  "schemaVersion": 1,
  "featurePath": "specs/feature-name",
  "notes": [
    { "kind": "criterion", "id": "AC-001", "text": "Please clarify the user-facing fallback." }
  ]
}
```

The generated browser page creates this download locally. The workbench does not send notes to a service or apply them to repository files.
