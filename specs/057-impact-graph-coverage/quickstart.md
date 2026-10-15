# Quickstart: Impact Graph Coverage

Build and run the focused caller-impact checks:

```powershell
dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~CallImpactAnalyzerTests
dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~SemanticAnalyzer
```

Run impact against BrownPants with the maximum public sample:

```powershell
dotnet run --project src/BrownPants.Cli -c Release -- impact . `
  --changed examples/brownfield-bug-lab/managed/Workflows/OrderImportWorkflow.cs `
  --format markdown --limit 10000
```

Check that managed callers and the separate native interop chain both appear. The report may still be incomplete because unresolved calls or MSBuild workspace issues are independent of call-edge sampling. The implementation's internal graph cap is separate from the public `--limit` and is reported if reached.
