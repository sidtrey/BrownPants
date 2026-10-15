# Verification: Native Call Impact

**Acceptance criteria SHA-256**: `8a1280bad6a2b29e38210a1847f56aba5401e46c655d298381bf557b9ae35c5e`

## Build and tests

- `dotnet build BrownPants.sln -c Release --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build` — 268 passed, 0 failed, 0 skipped.
- `tests/SpeckitExtensionTests.ps1` — passed; lifecycle hooks and live Spec Kit CLI extension checks succeeded.
- `tests/OpenSpecInstallerTests.ps1` — passed.

## BrownPants-on-BrownPants evidence

Ran the Release CLI against the repository root with `examples/brownfield-bug-lab/legacy/Bridge/OrderManagedBridge.cpp` as the changed path.

- `impact` returned **Analysis complete: yes**, with two native candidate links: the qualified call at `OrderMfcView.cpp:11` (depth 1) and the MFC `ID_ORDER_IMPORT_BATCH` dispatch at `OrderMfcView.cpp:6` (depth 2).
- `evidence --changed` selected both native-flow findings and the C++/CLI bridge marker, reported `impactComplete: true`, and did not request managed semantic analysis.
- The packaged `BrownPants.Cli` 0.2.1 tool installed from the local package feed. `tests/PackedNativeImpactSmoke.ps1` passed against that installed tool; its checks covered candidate identity/status, path depth, native evidence focus, and unchanged fixture file hashes.
- `dotnet pack src/BrownPants.Cli -c Release --no-build --output ./artifacts/native-impact` created `BrownPants.Cli.0.2.1.nupkg`.

## Criteria and ledger

- `scripts/acceptance-criteria-hash.ps1` verified the frozen digest above.
- `scripts/check-acceptance-evidence.ps1` verified 8 tasks and 10 criteria. This checks structure and paths; it does not judge test adequacy.

## Evidence limits

Native findings are lexical candidates from recognized MFC message maps and explicitly qualified calls. They do not establish macro expansion, active preprocessor branches, overload binding, runtime dispatch, or a C++/CLI-to-C# call edge. Mixed requests can still report managed MSBuild workspace gaps separately.
