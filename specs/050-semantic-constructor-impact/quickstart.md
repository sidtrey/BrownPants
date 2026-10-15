# Quickstart: Semantic Constructor Impact

Default inspection remains syntax-only:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab --format markdown --limit 1000
```

For compiler-bound method and constructor edges in trusted projects:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab/managed --semantic --format json --limit 1000
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab/managed --changed Persistence/OrderRepository.cs --format markdown --limit 1000
```

Semantic mode loads MSBuild projects and may evaluate imported targets. Constructor findings are source-level binding evidence and do not prove runtime execution or DI-container activation.
