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

## Convergence: source offsets after line comments

- **External repository**: Microsoft [VCSamples](https://github.com/microsoft/VCSamples), archived repository, checkout commit `9e1d4475555b76a17a3568369867f1d7b6cc6126` with a sparse MFC/VC2017 checkout. BrownPants discovered 4,078 files and 128 native projects in the selected directories.
- **Observed defect**: `VC2010Samples/MFC/general/viewex/maindoc.cpp` maps `IDM_CHANGEDATA` at line 29 to `CMainDoc::OnChangeData`, defined at line 64. Before the fix, `impact` and focused `evidence` reported the target at line 22 because trailing `//` comments were removed without preserving their character offsets.
- **Fix and result**: Comment removal now replaces the trailing comment with equal-length whitespace. The regression asserts the message-map source at line 4, handler at line 8, and qualified call/target at lines 11/14 in a compact fixture. On VCSamples, source and packed Release CLI both report message-map line 29 and handler target line 64; impact is complete with one MFC candidate for the changed file.
- **Verification**: `dotnet test BrownPants.sln -c Release --no-restore` — 389 passed. `dotnet pack` and local installation of BrownPants.Cli 0.4.0 succeeded; the packed MFC impact check returned the same line 64. Frozen acceptance criteria remain unchanged.
