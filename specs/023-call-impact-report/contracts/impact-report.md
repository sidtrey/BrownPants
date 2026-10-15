# Call-Trace Impact Report Contract

## Command

`brownpants impact <repository> --changed <relative-source-path> [--changed <relative-source-path> ...] [--format markdown|json] [--limit 1..10000]`

## Behavior

- When a changed path is C# or Visual Basic, loads managed projects through MSBuildWorkspace. Native-only requests do not load managed projects.
- Changed paths must remain within the repository and refer to discovered C#, Visual Basic, or C/C++ source files.
- Direct and transitive resolved callers are reported once at shortest depth with call-site path/line and project.
- Test paths are labeled candidates using repository heuristics.
- JSON begins with `SchemaVersion: 1`; Markdown and JSON contain equivalent findings.
- `IsComplete` and issues communicate partial project loading, unresolved edges, bounded samples, unsupported inputs, and missing source mappings.
- Native changed paths use a separate candidate collection for supported lexical MFC dispatch and qualified native-call relations. These candidates are not compiler-resolved edges.
- JSON impact entries identify their evidence kind (`managed-compiler-resolved` or `native-lexical`) and keep native relation status separate (`candidate`, `unresolved`, or `ambiguous`).
- The command does not run tests, builds, or arbitrary commands and does not modify the repository. MSBuild workspace loading may evaluate imports or design-time targets.

## Evidence limits

Caller counts and maximum depth are observed graph measures, not effort or story-point estimates. Test candidate paths do not prove coverage. Managed relationships depend on the loaded MSBuild configuration. Native edges remain lexical candidates; macro expansion, active preprocessor branches, overload binding, unqualified calls, runtime behavior, and C++/CLI-to-C# linkage may be absent from the graph.
