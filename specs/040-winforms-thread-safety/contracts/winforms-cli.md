# WinForms Thread-Safety Candidate CLI Contract

```text
brownpants winforms analyze <repository> [--format markdown|json] [--limit 1..10000]
```

- Reads only repository C# and Visual Basic source discovered by the standard scanner.
- Emits stable, bounded findings with path and line/column, receiver/property identity, Control-type evidence, observed background callback, and marshaling markers.
- Default Markdown output; JSON starts with `SchemaVersion: 1`.
- No credentials, network, MSBuild project loading/evaluation, build, code execution, or file writes occur.
- Findings are syntax-based candidates. `Unknown` execution context does not mean safe; `InvokeRequired` does not prove a guarded write; dispatcher syntax does not prove successful runtime marshaling.
- Output contains no source-line excerpts. Parse diagnostics and omission counts are explicit.
