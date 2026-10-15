# Data Model: Semantic Resolution Coverage

## Semantic analysis summary

The existing semantic analysis object gains additive aggregate fields:

| Field | Meaning |
|---|---|
| `ProjectsDiscovered` | C# and Visual Basic project files found by repository inventory. |
| `ProjectsLoaded` | Distinct discovered projects whose compilation was loaded. |
| `ProjectsFailed` | Discovered project files that could not be loaded. |
| `ProjectCoverageComplete` | True only when every discovered project loaded and no workspace/project-load failure was observed. |
| `InvocationSyntaxCount` | Repository-contained invocation syntax nodes encountered in loaded compilations. |
| `SkippedInvocationCount` | Invocation nodes deliberately excluded from method binding, such as C# `nameof`. |
| `AnalyzedInvocationCount` | Invocation nodes queried for semantic method binding. |
| `ResolvedInvocationCount` | Analyzed invocations whose bound symbol is an `IMethodSymbol`. |
| `UnresolvedInvocationCount` | Analyzed invocations without a bound method symbol. |
| `UnresolvedReasonCounts` | Deterministically ordered totals keyed by Roslyn `CandidateReason`. |
| `WorkspaceIssueCount` | Total observed workspace/project load failure messages, regardless of sample limit. |
| `CompilerErrorCount` | Total error diagnostics returned by loaded project compilations. |
| `CompilerDiagnostics` | Bounded compiler error samples with project, optional repository source path/line, code, and message. |

Count invariants: `InvocationSyntaxCount = SkippedInvocationCount + AnalyzedInvocationCount`; `AnalyzedInvocationCount = ResolvedInvocationCount + UnresolvedInvocationCount`; and the sum of `UnresolvedReasonCounts` equals `UnresolvedInvocationCount`.

## Resolved call

The existing record remains `{ Project, Path, Line, Expression, Caller, Target, TargetPath, TargetLine }`. `TargetPath` and `TargetLine` are populated only for source locations inside the inspected repository; external and metadata methods retain null locations.

## Unresolved call

The existing record remains `{ Project, Path, Line, Expression, CandidateReason, CandidateTargets }`. `CandidateReason` groups the aggregate reason total. `CandidateTargets` is an example list from Roslyn, not a promise that one candidate is correct.

## Samples and output

All examples use the existing sample limit. Aggregate counts are computed over all analyzed repository projects and trees before sampling. Markdown and JSON carry the same semantic facts; Markdown renders compact explanations and source locations.
