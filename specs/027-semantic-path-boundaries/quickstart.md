# Quickstart: Semantic Source Path Boundaries

Run fixture-focused semantic tests:

```powershell
dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter FullyQualifiedName~SemanticAnalyzer
```

Run BrownPants on itself after restoring/building the solution:

```powershell
dotnet run --configuration Release --no-build --project src/BrownPants.Cli -- inspect . --semantic --format json --limit 10000
dotnet run --configuration Release --no-build --project src/BrownPants.Cli -- inspect . --semantic --format markdown --limit 10000
```

Check that the report retains repository call findings and does not contain the local `.nuget` source path. Semantic inspection loads MSBuild projects and may evaluate imported targets; use it only for trusted repositories.
