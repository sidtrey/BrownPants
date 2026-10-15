# Quickstart: Verify Unique Semantic Evidence

From the repository root:

```powershell
dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~Evidence
dotnet run --project src/BrownPants.Cli -c Release -- evidence examples/brownfield-bug-lab --semantic --format json --limit 1000
dotnet run --project src/BrownPants.Cli -c Release -- evidence examples/brownfield-bug-lab --semantic --changed managed/tests/Program.cs --format json --limit 1000
```

The fixture must include separate same-target constructor expressions sharing a source line. The full and focused JSON must contain both rows with unique IDs and columns. The Bug Lab report should emit all 38 resolved relationships with no false truncation at limit 1000.
