# Quickstart: Empty Collection Mapping Candidates

From the BrownPants repository root:

```powershell
dotnet test BrownPants.sln --no-restore --filter FullyQualifiedName~CSharpDroppedCollectionMappingAnalyzerTests
dotnet build BrownPants.sln --no-restore
$fullMetalRoot = Read-Host 'Path to the FullMetalFile_React checkout'
dotnet run --project src/BrownPants.Cli --no-build -- inspect $fullMetalRoot --format json --limit 10000
pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/080-empty-collection-mapping-candidates/spec.md
pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/080-empty-collection-mapping-candidates/spec.md -TasksPath specs/080-empty-collection-mapping-candidates/tasks.md -EvidencePath specs/080-empty-collection-mapping-candidates/acceptance-evidence.json
```

## FullMetalFile verification (2026-11-13)

The current scan used `--limit 100`, covered 327 C# source files, and reported 0 possible dropped-collection candidates, 0 omitted candidates, and 0 skipped files. In particular, it did not flag the repaired OCR layout mapping. This is a negative scan result; the generated positive fixture proves the analyzer still detects the documented syntax pattern.

The isolated feature branch built with 0 warnings and 0 errors, and the full suite passed 404/404.
