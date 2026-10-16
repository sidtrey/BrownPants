# Feature 071 Verification

## Baseline evidence

- The original BrownPants render of a generated 2,200-node / 2,199-edge chain created 4,308 tile PNGs and 4,308 editable XML files at 256-pixel tiles with 64-pixel overlap.
- Every original manifest entry resolved to nonempty PNG/XML files. First and final tile images were visually inspected.
- The original run used 7,468,399,374 bytes; each 1,729,641-byte sidecar repeated the entire graph. Its managed test output was later removed through the repository cleanup script after the defect was fixed and documented.

## Acceptance evidence

### SC-001 / SC-002 stress and round-trip

- Input: deterministic chain with 2,200 nodes and 2,199 edges. Options: 256-pixel tiles, 64-pixel overlap. Output canvas: 296 x 413,584 pixels.
- Reproduce with `pwsh -NoProfile -File scripts/qa-tile-bundle.ps1` (build Release first). Result: 4,308 PNG tiles, 4,308 tile-local `.drawio` files, one complete master source, and one manifest. Every manifest image/source path resolved. The master contains exactly 2,200 vertex cells and 2,199 edge cells.
- BrownPants' `LocalFlowchartDrawioParser.Parse` successfully imported all 4,308 tile sidecars; no parse failures. The independent unit tests also verify every tile edge's source and target cells exist, local page bounds match the image, translated geometry round-trips, and the complete master preserves all graph cells.
- XML bytes: 7,071,942 across tile excerpts plus 1,729,594 for the master = **8,801,536 bytes** total, below the 50,000,000-byte criterion. Repeating this master once per tile would use 7,451,090,952 XML bytes; the original run measured 7,451,293,428 bytes. PNG bytes were 14,579,671; total bundle artifacts were approximately 25.7 MB.
- Determinism: rendered the same 4,308-tile bundle twice with overwrite. SHA-256 hashes for all 8,618 bundle files (tiles, master, and manifest) were byte-identical.
- Visual spot check: first and final PNG tiles opened successfully. They show clipped chart portions at tile boundaries; adjacent images provide the declared 64-pixel continuity overlap.
- The successful generated output is under managed ignored `TestResults/.brownpants/runs/20261016T044945Z-tile-bundle-qa-b56e9f8e0151462f8219a16803596b8b/`; `scripts/qa-tile-bundle.ps1` recreates and validates it. The normal five-run / fourteen-day cleanup policy manages this output.

### Test and compatibility evidence

- Focused regression coverage checks tile-local XML re-import, themed tile output, same-output deterministic overwrite, master and sidecar conflicts, stale tile removal, and preservation of unrelated files.
- `dotnet build BrownPants.sln -c Release --no-restore`: succeeded with 0 warnings and 0 errors.
- `pwsh -NoProfile -File scripts/test.ps1`: **371 passed, 0 failed**, including explicit tests for renderer edge-style import, bounded negative coordinates, and rejection of unknown edge styles/out-of-bounds coordinates. Managed cleanup removed eligible expired successful runs and preserved unmanaged legacy `TestResults` entries.
- The configured Microsoft.Testing.Platform runner does not support the attempted VSTest `--filter` invocation; that command ran zero tests. The full repository script above is the verified test entry point.
- `dotnet list BrownPants.sln package --vulnerable --include-transitive`: no vulnerable packages in either the CLI or test project using the configured NuGet source.
- Chart-source audit found no `System.Drawing`, graphics toolkit, process-launch, or network APIs in chart source. `BrownPants.Cli.csproj` has no charting toolkit package reference; drawing, layout, PNG, and XML remain managed BrownPants/.NET code.
- `pwsh -NoProfile -File scripts/qa-chart-engine.ps1` used BrownPants on its own chart writer. Independent semantic impact and the generated caller chart agreed on 5 callers, 6 source call sites, and 12 XML edges; 2 test paths were labeled as candidates. The chart and command disclosed 13 unresolved delegate targets, so the chart did not hide that analysis gap. The generated PNG was opened for visual review.
- Linux cross-platform check used the cached Microsoft .NET 10 SDK container: locked restore and Release build succeeded with 0 warnings/errors; all 371 tests passed. Windows and Linux renders of the four-tile release pipeline had byte-identical hashes for all 9 PNG/XML files (4 PNGs, 4 tile excerpts, 1 master); normalized manifests matched across all fields, tile order, bounds, and overlap values.
- Acceptance hash check confirmed `44a60464d4fbaa468046015763d093674dd6ddbac4564369654ad18092c7f0c6`; evidence validator recognized 8 tasks and 11 criteria. These checks verify ledger structure; the commands and stress results above provide the behavioral evidence.
- Five-reviewer chart usability acceptance for Feature 068 remains independent of this storage feature.
