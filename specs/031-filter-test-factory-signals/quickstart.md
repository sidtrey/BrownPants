# Quickstart: Validate Test-Method Factory Filtering

From the repository root, run the focused scanner fixture test and the complete suite:

```powershell
dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~RepositoryScannerTests
dotnet test BrownPants.sln -c Release
```

The fixture tests should show zero `Factory method` findings for recognized xUnit, NUnit, and MSTest methods in C# and VB. Production factory methods with leading attributes/comments should remain candidates; their `Line` identifies the declaration and `Evidence` contains its signature. Existing string/void exclusions remain in place.

Inspect this repository in both formats to confirm the two submission tests are no longer factory findings:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect . --format json --semantic --limit 100
dotnet run --project src/BrownPants.Cli -- inspect . --format markdown --semantic --limit 100
```

Semantic inspection is used here to match the original observed report, but this feature itself requires no MSBuild evaluation. Search the architecture-pattern section for `CreateDraftAsync_SendsOneDraftCreateRequestWithExplicitWorkItems` and `CreateDraftAsync_RejectsEditedPreviewWithoutNetworkRequest`; neither should appear as a factory method. Confirm genuine factories such as `CreateHttpException` remain and cite their declaration lines.
