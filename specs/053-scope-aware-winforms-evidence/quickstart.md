# Quickstart: Scope-Aware WinForms Evidence

Run the source-only analyzer against a legacy repository:

```powershell
dotnet run --project src/BrownPants.Cli -- winforms analyze <repository> --format json --limit 1000
```

Review each candidate path/property and its type evidence. Check coverage fields when a likely receiver was not resolved. The analyzer does not evaluate `.csproj`/`.vbproj`, build, or establish thread safety.

Validate the implementation:

```powershell
dotnet test BrownPants.sln --filter FullyQualifiedName~WinFormsUiThreadingTests
dotnet test BrownPants.sln -c Release
dotnet build BrownPants.sln -c Release --no-restore
```

Run `tests/PackedWinFormsSmoke.ps1` with the locally packed tool against the legacy Framework fixture and verify it creates no files in that fixture.
