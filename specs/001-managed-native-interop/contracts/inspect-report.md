# CLI Report Contract

## Invocation

```text
brownpants inspect [path] [--format markdown|json] [--limit 1..10000] [--semantic]
```

- `path` defaults to the current directory.
- `--format` defaults to Markdown. JSON is indented serialization of the repository inventory.
- `--limit` defaults to 200 and caps sampled details, not aggregate counts.
- `--semantic` opts into MSBuildWorkspace loading for resolved C# and VB.NET invocation edges. Treat inspected project files as trusted before using this mode because imports and design-time targets may run.

## Report requirements

- Both formats describe the same inventory and findings. Markdown renders all samples present in the inventory records; `--limit` bounds those samples identically in both formats.
- `ProjectSystems` groups conventional project/build manifest paths by ecosystem while retaining `ProjectMarkers` for path-level consumers.
- `LanguageCoverage` reports source file counts and the actual analysis level: Roslyn syntax/candidate analysis, lexical candidate patterns, or inventory-only.
- `Extensions` retains unfamiliar suffixes and uses `[no extension]` for extensionless files so aggregate counts reconcile with `FileCount`.
- Paths are repository-relative; source line numbers are one-based.
- Output is stably ordered for unchanged input.
- Pattern findings include category, evidence kind, language, path, line, and evidence. `Candidate` signals do not imply a confirmed architecture edge; Roslyn-resolved managed calls and project metadata remain separate report sections.
- Reports disclose parser, project-load, and sample-limit constraints where relevant.
- Invalid options return exit code 2 with a usage error. An inspection I/O or argument failure returns exit code 1. Successful inspection returns 0.

## Compatibility

JSON consumers should tolerate additive inventory fields and bounded samples. Existing report fields and their meanings should remain stable; changes that alter serialization shape require updating both representative JSON fixtures and the Spec Kit context guidance.
