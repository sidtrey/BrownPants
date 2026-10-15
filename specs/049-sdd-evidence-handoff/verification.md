# Verification: SDD Evidence Handoff

## Acceptance criteria

- **AC-001 / SC-001**: `brownpants evidence examples/brownfield-bug-lab --format json --limit 1000` produced schema version 1 with deterministic ordering, repository/language context, source-located candidate signals, test paths, and completeness/limitations. Repeated invocation produced byte-identical output.
- **AC-002**: The paired run's semantic impact report resolved four managed calls; MFC/native and ORM relationships remained candidates.
- **AC-003**: All pattern and test-path findings from the evidence command were labeled `candidate`; managed calls were labeled `resolved` only from Roslyn evidence.
- **AC-004**: The output uses stable identity hashes and deterministic ordering, and de-duplicates identical IDs while retaining distinct source lines.
- **AC-005 / SC-004**: The v3 paired comparison scored baseline at 5/20 TP and 6 FP (25% recall, 45.5% precision) and assisted at 20/20 TP and 7 FP (100% recall, 74.1% precision). The 75-point recall gain and higher precision pass the frozen bar. The assisted structured result excluded both frozen decoys, and candidate findings stayed labeled `candidate`. Full counts and raw agent findings are in `docs/comparisons/brownfield-evidence-handoff-2026-10-results.json`.

## Checks run

- `dotnet build BrownPants.sln` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln --no-build --no-restore` — passed, 243 tests.
- `dotnet pack src/BrownPants.Cli/BrownPants.Cli.csproj -c Release -o TestResults/nuget-evidence-smoke` — passed; extracted package assembly ran `evidence` and `evidence --help` successfully.
- `Score-Comparison.ps1 -SelfTest` — passed, 20 expected findings and 2 decoys.
- `Prepare-Comparison.ps1` — both seeded prechecks failed with `Batch import saved 1 of 2 orders.` and identical seeded source hashes.
- Same Codex CLI 0.162.1, `gpt-6-luna`, and v3 task prompt in both isolated copies — both agents verified acceptance hash `777ecb0e7523c67dc4a3e8b28cebae70e7b9476939c00a65eea50308c7d3dd63`, produced identical fixes, and reported the focused checks passed.
- Independent rerun of `dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release` in both copies — passed with `LegacyOrderDesk checks passed.`
- Feature acceptance hash `22cc2282731a63b7391ec0ba83074d22e451d07db9ac0e7a3f551bf9d2899113` — verified before and after implementation.
- Acceptance evidence ledger validator — passed for 8 tasks and 9 criteria.

## Limits

The full fixture's semantic scan reports incomplete project coverage because the analysis-only legacy project is outside the supported semantic build path. The controlled impact run intentionally used the package-free managed project. Seven assisted false positives remain, all in framework candidates; they are disclosed in the comparison results.
