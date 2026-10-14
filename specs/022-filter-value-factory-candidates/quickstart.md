# Quickstart: Verify Factory Candidate Filtering

Run the focused fixtures:

```powershell
dotnet test BrownPants.sln --configuration Release --filter FullyQualifiedName~Factory
```

Then scan BrownPants itself:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect . --format json --limit 100
```

Expected: `ArchitecturePatterns.Signals` has no `Factory method` signal for string-returning `Create`, `Build`, or `Resolve` helpers, including `ResolveCustomActiveWorkItemPath`; object-returning C# and VB.NET factory fixtures remain present with candidate evidence and source locations. Run `dotnet test BrownPants.sln --configuration Release` before completion.
