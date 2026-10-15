# Quickstart: Validate the Legacy Semantic Fixture

Run from the repository root on Windows with .NET Framework 4.7.2 targeting assemblies installed. Semantic inspection is opt-in and evaluates imported MSBuild targets.

```powershell
dotnet restore examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj
dotnet build examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj --no-restore
dotnet run --project src/BrownPants.Cli -c Release -- inspect examples/brownfield-bug-lab/legacy/Framework --semantic --format json --limit 1000
dotnet run --project examples/brownfield-bug-lab/managed/tests/LegacyOrderDesk.Checks.csproj -c Release
dotnet build BrownPants.sln -c Release
dotnet test BrownPants.sln -c Release
```

The semantic JSON should show the legacy project loaded without workspace or compiler errors and resolve the four NHibernate invocations. Package restore and Framework targeting prerequisites may vary by machine. The managed regression check is independent of the legacy NHibernate project.
