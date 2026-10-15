# Quickstart: SDD Evidence Handoff

From the repository root:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence . --format json --limit 1000
```

This runs static analysis and writes one JSON report to stdout. To add Roslyn call-target resolution for trusted project files:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence . --format json --limit 1000 --semantic
```

Semantic mode loads MSBuild projects and may evaluate imported build targets. Agents should preserve `resolved` and `candidate` labels and include relevant limitations in task results.
