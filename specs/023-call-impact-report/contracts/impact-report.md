# Call-Trace Impact Report Contract

## Command

`brownpants impact <repository> --changed <relative-source-path> [--changed <relative-source-path> ...] [--format markdown|json] [--limit 1..10000]`

## Behavior

- Explicitly loads C# and Visual Basic projects through MSBuildWorkspace.
- Changed paths must remain within the repository and refer to discovered source files.
- Direct and transitive resolved callers are reported once at shortest depth with call-site path/line and project.
- Test paths are labeled candidates using repository heuristics.
- JSON begins with `SchemaVersion: 1`; Markdown and JSON contain equivalent findings.
- `IsComplete` and issues communicate partial project loading, unresolved edges, bounded samples, unsupported inputs, and missing source mappings.
- The command does not run tests, builds, or arbitrary commands and does not modify the repository. MSBuild workspace loading may evaluate imports or design-time targets.

## Evidence limits

Caller counts and maximum depth are observed graph measures, not effort or story-point estimates. Test candidate paths do not prove coverage. Reflection, dynamic dispatch, conditional compilation, runtime behavior, and native C++/MFC relationships may be absent from the graph.