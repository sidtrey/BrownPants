# Verification: C# Consumption of C++/CLI Libraries

Run from `F:\temp\brownpants-feature-074` on 2026-11-12.

## Results

- `dotnet restore BrownPants.sln --locked-mode` — passed.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build` — passed, 395 passed, 0 failed, 0 skipped.
- `dotnet pack src/BrownPants.Cli/BrownPants.Cli.csproj -c Release --no-restore -o $env:TEMP/brownpants-074-pack-final` — passed; created `BrownPants.Cli.0.4.0.nupkg`.
- Installed the package from the local feed with an isolated NuGet config, then ran `pwsh -NoProfile -File tests/PackedManagedInteropSmoke.ps1 -ToolPath $env:TEMP/brownpants-074-tool-final/brownpants.exe` — passed. The smoke covers packed inspect, C#-first focused evidence, changed-consumer impact, and source immutability.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/074-csharp-cli-consumption/spec.md` — passed; SHA-256 `7a6b46874fefbc45ff9a1dfeb4fa8acf43d4bf9bb3d8332e49ef822ada272f92`, matching the initialized baseline.
- `git diff --check` — passed.

## Evidence review

The source-only fixture produces a `candidate` `csharp-cppcli-call` at `legacy/ManagedConsumer/LegacyConsumer.cs:8`, pointing to `legacy/Bridge/StlClrLibrary.cpp:11`. Focused changed-path evidence also includes a separate `candidate` `qualified-native-call` at wrapper line 13 pointing to the qualified native definition at line 2. Its impact remains incomplete for managed caller relationships without `--semantic`.

Project association uses static legacy `Reference Include`, `HintPath`, `TargetName`, and discovered source paths. The scanner does not evaluate imported MSBuild targets or read binary paths. An external/traversing `HintPath` produces an unresolved relation and is not read. Repeated scans serialize identically; ambiguity retains both matching wrapper locations, while an unreferenced same-name decoy is excluded. Candidate labels and report limitations do not claim compiler binding, binary provenance, runtime loading, or execution.

## Known limits

The C# call index supports explicit local receiver types, local variables initialized with `new`, and explicit object-creation receivers. Dynamic, alias-only, extension, overload-binding, and other unsupported forms are not resolved. C++/CLI source parsing remains bounded lexical evidence; preprocessor selection, overload binding, macro expansion, project configuration selection, and runtime dispatch are not resolved.
