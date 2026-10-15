# Quickstart: Nested Callable Identities

Run from the repository root:

```powershell
dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter FullyQualifiedName~SemanticAnalyzer
dotnet run --project src/BrownPants.Cli -c Release --no-build -- impact examples/brownfield-bug-lab --changed managed/Workflows/OrderImportWorkflow.cs --format json --limit 1000
dotnet run --project src/BrownPants.Cli -c Release -- evidence examples/brownfield-bug-lab --semantic --changed managed/Workflows/OrderImportWorkflow.cs --format json --limit 1000
```

Expected results: calls inside `OrderBatchFactory.Register`'s lambda identify the named registration method and a distinct source-relative lambda anchor. Local functions retain parent-qualified identity. Existing managed callers, resolved targets, constructor relationships, and deterministic output remain intact.
