# Quickstart: Source-Backed Constructor Declarations

Run the focused semantic and impact tests:

```powershell
dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter "FullyQualifiedName~ImplicitConstructor|FullyQualifiedName~PrimaryConstructor"
```

Run the full release gates:

```powershell
dotnet build BrownPants.sln -c Release
dotnet test BrownPants.sln -c Release --no-build
```

On a trusted repository, opt-in semantic analysis to check the end-to-end call graph:

```powershell
dotnet run --project src/BrownPants.Cli -c Release --no-build -- impact examples/brownfield-bug-lab --changed managed/Workflows/OrderImportWorkflow.cs --format json --limit 1000
```

Semantic mode loads MSBuild projects and may evaluate imported build targets. Constructor edges show compiler-bound source relationships, not runtime execution.
