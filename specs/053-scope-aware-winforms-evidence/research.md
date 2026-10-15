# Research: Scope-Aware WinForms Control Evidence

## Decision 1: Keep the analyzer source-only

- **Decision**: Resolve known Control types from C#/VB syntax declarations and source metadata, without loading an MSBuild workspace.
- **Rationale**: Feature 040 promises legacy framework analysis without project evaluation. An optional workspace could improve type binding but risks evaluating imported targets and would fail on many old projects.
- **Alternatives considered**: Add a `--semantic` option; rejected for this improvement because it would create a second trust mode and still leave syntax-only legacy coverage incomplete.

## Decision 2: Use qualified type identities and lexical symbol lookup

- **Decision**: Index source types by namespace-qualified names, merge partial declarations, and resolve receiver names at each assignment using nearest lexical declarations followed by members on the containing source type.
- **Rationale**: The current analyzer scans each file independently and flattens variables into a name dictionary. This misses split controls and permits scope collisions.
- **Alternatives considered**: Expand built-in name lists or add more string heuristics; rejected because those do not handle shadowing, duplicate simple names, or chained typed members reliably.

## Decision 3: Treat unresolved candidates as coverage information

- **Decision**: Do not infer Control type from a property name or ambiguous identifier. Emit bounded coverage issues/counts for syntactically plausible writes that cannot be resolved.
- **Rationale**: False claims reduce trust; reviewers need to distinguish no match from incomplete evidence.
- **Alternatives considered**: Report all member assignments as possible Controls; rejected because generic models and ordinary objects would flood output.

## Source evidence

- `src/BrownPants.Cli/Analysis/WinFormsUiThreadingAnalyzer.cs`: `AnalyzeCSharp` and `AnalyzeVisualBasic` build file-local maps; C# `variableTypes` is keyed only by identifier; VB `variables` is a whole-file case-insensitive dictionary.
- `specs/040-winforms-thread-safety/spec.md`: aliases, partial classes, inherited controls, and fields in another file are identified as cases that can prevent local syntax recognition.
- `specs/040-winforms-thread-safety/verification.md`: prior evidence establishes parser/CLI behavior but not cross-file symbol resolution.
