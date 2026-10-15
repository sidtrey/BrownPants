# Verification: CLI Command Help

## Acceptance criteria

All acceptance scenarios are covered by captured-output tests in `tests/BrownPants.Tests/RepositoryScannerTests.cs`; existing tests also cover the `help` global alias and instructions retrieval help.

## Commands and results

- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter FullyQualifiedName~Help_` — passed, 12 tests.
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 100 tests.
- `pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/028-cli-command-help/spec.md -TasksPath specs/028-cli-command-help/tasks.md -EvidencePath specs/028-cli-command-help/acceptance-evidence.json` — passed; 3 tasks and 9 criteria structurally covered.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/028-cli-command-help/spec.md` — passed; SHA-256 `4aaf47e6810f0f4736b78e0650eeef3f30fcda386122e0f24164ab8b3aed62d1` unchanged since initialization.
- BrownPants self-use: `dotnet run --configuration Release --no-build --project src/BrownPants.Cli -- inspect . --format markdown --limit 10` — passed; identified the .NET solution, active feature 028, language inventory, and evidence limits.

Cross-platform CI was not run locally. Static help routing occurs before command argument parsing and does not inspect repositories, evaluate projects, or write files.
