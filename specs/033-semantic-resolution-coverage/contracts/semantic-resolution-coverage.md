# Semantic Resolution Coverage Contract

## Commands

- `brownpants inspect <repository> [--format markdown|json] [--limit 1..10000]`
- Add `--semantic` to load discovered C# and Visual Basic projects through MSBuildWorkspace and bind method invocations.

## Contract

- Without `--semantic`, invocation-expression names are syntax-only. No project load or method-target resolution occurs.
- Semantic JSON is additive under `CSharp.Semantic` and remains schema version 1. Existing fields keep their current meaning.
- Semantic totals obey:
  - `InvocationSyntaxCount = SkippedInvocationCount + AnalyzedInvocationCount`
  - `AnalyzedInvocationCount = ResolvedInvocationCount + UnresolvedInvocationCount`
  - `sum(UnresolvedReasonCounts) = UnresolvedInvocationCount`
- `TargetPath` and `TargetLine` are non-null only for repository-contained source definitions.
- Discovered project files are attempted once across solutions and standalone loading. Project coverage is complete only when every discovered project loads and no workspace/project-load failures were observed.
- `WorkspaceIssueCount` and `CompilerErrorCount` describe total observations; sample arrays contain no more than `--limit` records and expose how many examples were omitted.
- Unresolved bindings and compiler errors are evidence, not a diagnosis of runtime behavior.

## Evidence limits

Binding reflects the MSBuild configuration loaded by Roslyn. Multi-target configurations may be represented by only the active/default target loaded for a project. Reflection, dynamic dispatch, preprocessor configurations not loaded, runtime behavior, and native C++/MFC relationships may not appear. Loading projects may evaluate imports and design-time targets.
