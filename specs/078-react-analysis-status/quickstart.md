# Feature 078 verification

## FullMetalFile default inspection

```powershell
$fullMetalRoot = Read-Host 'Path to the FullMetalFile_React checkout'
dotnet run --no-build --project src/BrownPants.Cli -- inspect $fullMetalRoot --format json --limit 1000
```

- BrownPants identified one React project and scanned 26 JavaScript/TypeScript source files.
- Before this change, the same report exposed `ComponentCount: 0` and `HookCallCount: 0` while `SyntaxVerification` was null. The UI source had not been parser-analyzed.
- After this change, JSON reports `CandidateAnalysisStatus: not-requested`; Markdown says candidate counts are unavailable and points to `--typescript-syntax`. Default inspection still does not invoke Node.

## FullMetalFile parser-verified inspection

```powershell
dotnet run --no-build --project src/BrownPants.Cli -- inspect $fullMetalRoot --typescript-syntax --format json --limit 1000
```

- The local TypeScript 5.9.3 parser verified 26 files with zero parser diagnostics.
- JSON and Markdown both report candidate analysis status `verified`, 12 function component candidates, and 90 hook-call candidates with source locations.
- These remain syntax candidates; they do not prove runtime component use.

## BrownPants implementation checks

- `dotnet build BrownPants.sln --no-restore`: succeeded with 0 warnings and 0 errors.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj --no-restore --filter FullyQualifiedName~TypeScriptSyntaxVerifierTests`: 9 passed, 0 failed, 0 skipped. Coverage includes verified zero-candidate and partial-analysis output.
- BrownPants changed-path evidence covered all 4 implementation/test paths; 2 matched findings, 5 emitted findings, and no truncation. See `brownpants-evidence.json`.
- FullMetalFile JSON and Markdown reports are retained under BrownPants `TestResults/feature-078-fullmetal-*` as ignored local evidence.
- The acceptance digest and evidence ledger were verified after implementation. The full BrownPants test suite was not run.
