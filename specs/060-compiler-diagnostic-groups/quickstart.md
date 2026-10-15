# Quickstart: Compiler Diagnostic Groups

From the repository root:

```powershell
dotnet test BrownPants.sln -c Release --filter "FullyQualifiedName~CompilerDiagnosticGroup"
dotnet run --project src/BrownPants.Cli -- inspect . --semantic --format json --limit 10000
dotnet run --project src/BrownPants.Cli -- inspect . --semantic --format markdown --limit 10
```

The fixture should report deterministic project/code group counts and retain existing compiler error totals and source-located diagnostic samples. BrownPants' current self-analysis should show four groups with counts 12, 4, 3, and 2. Those frequencies are investigation leads, not causes. See [the report contract](contracts/compiler-diagnostic-groups.md) and [data model](data-model.md).
