# Compiler Diagnostic Groups Contract

Semantic JSON adds three fields under the existing C# semantic result:

- `CompilerDiagnosticGroups`: sampled `{ Project, Code, Count }` objects, sorted by normalized project path and then code.
- `CompilerDiagnosticGroupCount`: exact number of distinct project/code groups.
- `CompilerDiagnosticGroupOmittedCount`: groups omitted after applying `--limit`.

The JSON `SchemaVersion` remains `1`; these are additive fields. Existing `CompilerErrorCount`, `CompilerErrorOmittedCount`, and `CompilerDiagnostics` remain unchanged. Markdown shows grouped counts and continues to show individual diagnostic samples. Empty groups serialize as an empty list with zero total and omitted counts. The group table does not diagnose causes or recommend fixes.
