# Quickstart: CLI Command Help

```powershell
dotnet run --project src/BrownPants.Cli -- --help
dotnet run --project src/BrownPants.Cli -- inspect --help
dotnet run --project src/BrownPants.Cli -- impact -h
dotnet run --project src/BrownPants.Cli -- frame create --help
```

Inspect defaults to syntax-level invocation inventory. `--semantic` resolves invocation targets by loading MSBuild projects and may evaluate imported targets. Requesting help does not inspect a repository or execute an operation.
