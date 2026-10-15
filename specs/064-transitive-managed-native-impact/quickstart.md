# Quickstart: Transitive Managed-to-Native Impact

Run from the repository root after a Release build:

```powershell
dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~CallImpactAnalyzerTests
dotnet run --project src/BrownPants.Cli -c Release -- impact examples/brownfield-bug-lab --changed managed/Persistence/OrderRepository.cs --format json --limit 1000
dotnet run --project src/BrownPants.Cli -c Release -- impact examples/brownfield-bug-lab --changed managed/Workflows/OrderImportWorkflow.cs --format markdown --limit 1000
dotnet run --project src/BrownPants.Cli -c Release -- evidence examples/brownfield-bug-lab --semantic --changed managed/Persistence/OrderRepository.cs --format json --limit 1000
```

Expected results: the repository change includes the `ImportBatch` managed caller and its C++/CLI, qualified native, and MFC dispatch candidates; native depths remain their native-only distances and connected rows also identify their managed seed depth. Direct workflow impact retains current depth values. Focused semantic evidence includes those same candidate identities. All native links remain labeled lexical candidates.
