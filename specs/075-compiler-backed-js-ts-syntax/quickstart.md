# Quickstart: Compiler-Backed JavaScript and TypeScript Syntax Checks

## Default inspection

Default inspection remains dependency-free and does not start Node:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect <repository> --format markdown
```

## Opt-in parser verification

From the BrownPants repository, pass a repository that already contains a local TypeScript installation:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect <repository> --typescript-syntax --format markdown --limit 1000
dotnet run --project src/BrownPants.Cli -- inspect <repository> --typescript-syntax --format json --limit 1000
```

The option loads the nearest applicable repository-local TypeScript compiler package through Node and parses recognized JS/TS source files. In a React package it also reports syntax-based component and hook-call candidates. It does not install packages, run package scripts, load TypeScript project configuration, type-check, or build. Review the parser path and version in output before trusting it. If no parser or Node runtime is available, the report says verification was unavailable and keeps the bounded heuristic findings.

## FullMetalFile verification

```powershell
$fullMetalRoot = Read-Host 'Path to the FullMetalFile_React checkout'
dotnet run --project src/BrownPants.Cli -- inspect $fullMetalRoot --typescript-syntax --format json --limit 1000
node --check (Join-Path $fullMetalRoot 'scripts/authz-matrix.mjs')
```

Compare BrownPants parser diagnostics with the local TypeScript parser's `parseDiagnostics`. Syntax verification does not establish type correctness, successful bundling, module resolution, runtime behavior, or test coverage.

## Current FullMetalFile result (2026-11-13)

BrownPants scanned the current `FullMetalFile_React` checkout and verified 27 JavaScript/TypeScript files with the locally installed TypeScript 5.9.3 parser at `FullMetalFile.Client/node_modules/typescript/lib/typescript.js`. It reported zero parser diagnostics and 15 separate syntax heuristics. React metadata identified `fullmetalfile-client`; syntax analysis reported 12 function-component candidates and 135 hook-call candidates. These are navigation candidates, not proof of React binding, runtime use, or correctness.

The default scan was also rerun against the same checkout. `SyntaxVerification` was null, all 15 syntax findings remained labeled heuristics, and React candidate analysis was `not-requested` with zero emitted candidates. This confirms that default mode did not invoke Node or present unrequested candidates as an analyzed zero.

A temporary malformed TS source probe produced TypeScript diagnostic 1134 at line 1, columns 14 and 16 with message `Variable declaration expected.`; the probe file was removed immediately. `node --check scripts/authz-matrix.mjs` passed.

Current isolated-branch quality gates passed:

- `dotnet build BrownPants.sln`: 0 warnings, 0 errors.
- `dotnet test BrownPants.sln --no-build`: 414 passed, 0 failed, 0 skipped.
- Fresh opt-in JSON and Markdown reports identify the same TypeScript parser and show 27 verified files, 0 parser diagnostics, 12 component candidates, and 135 hook-call candidates.
- `dotnet pack` produced the 0.4.0 tool package; it was installed from a local-only NuGet feed. The packed JS/TS smoke passed, and the packed tool's `--typescript-syntax` run against FullMetalFile also verified 27 files with 0 diagnostics, 12 components, and 135 hook calls.

The earlier FullMetalFile client `npm run build` passed on 2026-11-02 (`tsc` and Vite, 1,605 modules transformed); it was not rerun for this parser-only change. Reviewable local reports and logs are under BrownPants `TestResults/`: `feature-075-fullmetal-react-parser.json`, `feature-075-fullmetal-react-parser.md`, `feature-075-fullmetal-default.json`, `feature-075-packed-fullmetal-react-parser.json`, `feature-075-packed-tool-smoke.log`, `feature-075-dotnet-build.log`, `feature-075-dotnet-test.log`, and `feature-075-fullmetal-client-build.log`. These reports include local paths and are ignored generated evidence.

## Follow-up: distinguish tokenizer heuristics in default reports (2026-11-06)

The default FullMetalFile report now labels lexical parse-shape findings as `Syntax heuristic (not compiler-verified)` in Markdown and `EvidenceKind: SyntaxHeuristic` in JSON. Actual metadata, parser-execution, and resource-limit problems retain `EvidenceKind: Analysis` and the “Analysis issue” label. This keeps default inspection process-free while preventing a delimiter/tokenizer candidate from reading as a compiler diagnostic.

The updated default scan found 12 lexical syntax heuristics; explicit TypeScript 5.9.3 verification parsed 22 JS/TS/TSX files with 0 parser diagnostics. The JSON report classifies all 12 as `SyntaxHeuristic`, and its parser verification reports zero remaining analysis issues. In the current rerun, `dotnet build BrownPants.sln` passed with 0 warnings and 0 errors, and `dotnet test BrownPants.sln --no-build` passed all 434 tests.
