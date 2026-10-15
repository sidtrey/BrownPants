# Quickstart: Semantic Resolution Coverage

Run ordinary inspection to see syntax-level invocation names only:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect . --format markdown --limit 20
```

Resolve C# and Visual Basic method calls through loaded project configurations:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect . --semantic --format markdown --limit 20
dotnet run --project src/BrownPants.Cli -- inspect . --semantic --format json --limit 100
```

Review project coverage, invocation count identities, resolved target paths and lines, unresolved reason totals, workspace issue totals, and compiler errors. `--limit` bounds examples, not totals.

When a repository has multiple solutions, semantic loading uses one stable solution context and opens discovered projects outside it directly; project paths are analyzed once. Workspace issues can mark coverage incomplete even when MSBuild returned a compilation for the project.

Semantic inspection is opt-in because MSBuild imports and design-time targets may execute. Use it on repositories whose project files and imports you trust. An unresolved call, project issue, or compiler error needs source/project review and does not prove a runtime bug.
