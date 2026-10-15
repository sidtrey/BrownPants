# Quickstart: Check Workspace Diagnostic Normalization

Run the focused tests and inspect semantic output:

```powershell
dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~WorkspaceDiagnostic

dotnet run --project src/BrownPants.Cli -c Release -- inspect . --format json --semantic --limit 10000
```

Confirm that the `Compile`-target issue still names its repository-relative `.csproj` and preserves the cause, while the absolute checkout root is absent. The issue count and semantic schema remain unchanged.
