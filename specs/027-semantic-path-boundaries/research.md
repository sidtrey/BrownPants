# Research: Semantic Source Path Boundaries

## Current Behavior

- `CSharpSemanticAnalyzer.AnalyzeCompilations` iterates every syntax tree in each compilation and adds declarations and invocation findings without checking whether the tree is inside the scanned root.
- Its `RelativePath` helper calls `Path.GetRelativePath` for rooted paths. On a different Windows volume that can return an absolute path; for an external path on the same volume it can return parent traversal.
- Resolved target locations are also converted with that helper. BrownPants-on-BrownPants emitted `C:/Users/sidtr/.nuget/packages/microsoft.net.test.sdk/17.14.1/build/net8.0/Microsoft.NET.Test.Sdk.Program.cs` as a semantic declaration.
- Roslyn needs external references for binding. Filtering findings after compilation preserves the ability to resolve a repository call to an external method.

## Decision

Add one root-containment check for project and syntax-tree paths before creating semantic findings. Count and sample declarations/calls only for repository-contained source trees. For targets, report a source path and line only when a source location is inside the repository; keep the resolved symbol regardless. Normalize accepted paths to repository-relative forward-slash form. Do not change MSBuild workspace loading or the versioned report schema.

## Alternatives Considered

- **Redact absolute prefixes only**: rejected because same-volume paths can leak through `../` segments and generated external source would still pollute counts.
- **Disable source generators or package source**: rejected because compiler configuration varies and the analyzer should work from resolved compilations without changing project semantics.
- **Drop calls to external methods**: rejected because their method symbols still provide useful call-tracing evidence.
