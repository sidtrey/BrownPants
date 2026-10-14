# Feature 022 Verification

Verification ran on Windows with PowerShell 7 and the .NET 10 SDK.

| Check | Command | Observed result |
|---|---|---|
| Regression before fix | `dotnet test BrownPants.sln --configuration Release --filter FullyQualifiedName~Scan_ExcludesStringHelpersAndKeepsObjectFactoryMethodsInCSharpAndVisualBasic` | Failed as expected: the new fixture reproduced string-returning C# and VB.NET factory-method candidates. |
| Focused factory fixtures | `dotnet test BrownPants.sln --configuration Release --filter FullyQualifiedName~Factory` | Passed; 8 passed, 0 failed. |
| Release build | `dotnet build BrownPants.sln --configuration Release` | Passed; 0 warnings, 0 errors. |
| Full xUnit suite | `dotnet test BrownPants.sln --configuration Release --no-build` | Passed; 76 passed, 0 failed, 0 skipped. |
| Acceptance criteria baseline | `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/022-filter-value-factory-candidates/spec.md` | Passed; digest `fa050af74718d9b2d2faf6c9f58877f658480ff7629310b65d20dde5a4cb623c`. |
| Packed Release tool | Pack `BrownPants.Cli` 0.1.0, install to a temporary tool path, then run `brownpants inspect . --format json --limit 100` | Passed; schema v1, 263 files, and the packaged executable no longer reports the string path resolver as a factory method. |
| BrownPants self-inspection | `dotnet run --project src/BrownPants.Cli -- inspect . --format json --limit 100` | Passed; schema v1, 263 files, 0 C# parse errors, active feature 022, and 0 architecture signals. `ResolveCustomActiveWorkItemPath` is no longer reported. |

The new fixture verifies both JSON and Markdown candidate parity, including source path, line, and candidate evidence for retained C# and VB.NET object-returning methods.
