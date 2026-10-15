# Quickstart: Validate Local Delegate Impact

The analyzer runs under semantic mode, which loads MSBuild projects and may evaluate trusted project imports/targets. Use only repository sources you trust.

After implementation, run focused semantic/impact tests and the full release gates:

```powershell
dotnet test BrownPants.sln -c Release --filter-class BrownPants.Tests.RepositoryScannerTests
dotnet test BrownPants.sln -c Release --filter-class BrownPants.Tests.CallImpactAnalyzerTests
dotnet build BrownPants.sln -c Release
dotnet test BrownPants.sln -c Release
```

Fixture checks should demonstrate a Roslyn-selected overloaded method group, shorthand and explicit `Invoke`, C# and VB method groups, lambda traversal through the enclosing callable, and unsafe reassignment/capture/escape/parameter cases that report unresolved targets and incomplete impact. Re-run the same fixture and compare Markdown/JSON output byte-for-byte.

The approved release comparison gate requires three matched pairs. Freeze the task prompt, answer-key digest, and acceptance hash before each run; keep the scorer outside both source copies; counterbalance run order; and retain raw trial events/results and usage. Do not claim superiority if the predeclared threshold or regression checks fail.
