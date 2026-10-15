# Verification: Semantic Constructor Impact

- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/050-semantic-constructor-impact/spec.md` — acceptance hash verified (`faeec3e0260ce1beb3d10b868537dd0627fb4dc20ba0bc7e341597c587a477c9`).
- `dotnet test BrownPants.sln --no-restore` — passed, 245 tests.
- `dotnet test BrownPants.sln --configuration Release` — passed, 245 tests, 0 failed.
- `dotnet pack src/BrownPants.Cli/BrownPants.Cli.csproj --configuration Release --no-restore` — produced `BrownPants.Cli.0.2.1.nupkg` (13,407,456 bytes) in the temporary output directory.
- Installed that package into a temporary tool path and ran semantic inspection on `examples/brownfield-bug-lab/managed`: 2/2 projects loaded, 17 method invocations resolved, 17 constructor syntax nodes resolved, 0 unresolved constructors.
- Ran the packed CLI against BrownPants itself: inventory reported 660 files and 5/5 managed projects loaded; semantic analysis reported 8,752 resolved method invocations and 1,000/1,000 constructor syntax/resolved counts with `--limit 1000`. Project coverage remained incomplete because the workspace reported issues; BrownPants preserves that limitation in its output.
- Ran default static inspection on BrownPants itself; `CSharp.Semantic` was absent, so no resolved semantic edges were presented without `--semantic`.
- `scripts/check-acceptance-evidence.ps1` and `git diff --check` are run as part of final review.

The packed CLI smoke used a temporary NuGet configuration with the local package and nuget.org sources after the machine-level source mapping rejected `--add-source`. No repository or user NuGet configuration was modified.
