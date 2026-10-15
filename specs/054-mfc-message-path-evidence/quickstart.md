# Quickstart: Verify MFC Message Path Evidence

From the repository root:

```powershell
dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~RepositoryScannerTests
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab --format markdown --limit 1000
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab --format json --limit 1000
```

The fixture should show a candidate from `OrderMfcView.cpp:6` to the `OnImportBatch` definition at line 9, then a candidate call from line 11 to `OrderManagedBridge.cpp:5`. JSON must include target locations and candidate statuses. Native paths must not appear as Roslyn-resolved C# calls. Repeating either report against unchanged inputs must produce stable relation identities and ordering.
