# Quickstart: Managed Interop Impact

Build and run the existing verification suite:

```powershell
dotnet build BrownPants.sln -c Release
dotnet test BrownPants.sln -c Release --no-build
```

Inspect the mixed brownfield fixture without building its legacy projects:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab --format markdown --limit 1000
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab --changed managed/Workflows/OrderImportWorkflow.cs --format json --limit 1000
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab --changed managed/Workflows/OrderImportWorkflow.cs --format markdown --limit 1000
```

Expected reports include the managed `OrderImportWorkflow<T>.ImportBatch` declaration, a C++/CLI bridge-call candidate with a path/line, and upstream native/MFC candidates. If Roslyn semantic mode is explicitly requested for evidence, managed compiler-resolved callers remain separate. No report should call the bridge relation compiler-resolved.
