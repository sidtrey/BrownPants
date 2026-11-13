# Quickstart: C# No-op Implementation Candidates

From the BrownPants repository root:

```powershell
dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj --no-restore --filter FullyQualifiedName~CSharpImplementationPlaceholderAnalyzerTests
dotnet build BrownPants.sln --no-restore
dotnet test BrownPants.sln --no-restore
$fullMetalRoot = Read-Host 'Path to the FullMetalFile_React checkout'
dotnet run --project src/BrownPants.Cli --no-build -- inspect $fullMetalRoot --format json --limit 10000
```

The scan reports bounded source candidates only. It does not establish that a candidate is a defect or that the behavior is unintended.

## FullMetalFile check (2026-11-13)

The current scan covered 327 C# files (1,435,741 bytes), skipped none, and reported 17 candidates with none omitted: 2 production and 15 test candidates. `PostgreSqlUnitOfWork.CreateNotImplemented` appears at line 68 as a `NotImplementedException` candidate; the adjacent empty `Dispose` appears at line 64. The helper deliberately creates an exception for unimplemented repository accessors, so its report is a navigation lead, not an independent defect claim.

The original SC-004 example named PostgreSQL audit reads that FullMetalFile Feature 021 implemented before this analyzer was first run. The updated criterion uses the current helper candidate, which is covered by feature 079 itself and does not depend on the separate indirect-accessor extension. Fixture tests continue to cover empty results, default results, completed tasks, and direct `NotImplementedException` patterns.

The acceptance criterion was reviewed under the authorized one-maintainer waiver. The refreshed digest is recorded in `acceptance-criteria.sha256`; the review record does not claim independent approval.
