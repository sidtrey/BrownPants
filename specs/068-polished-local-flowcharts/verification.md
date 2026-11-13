# Feature 068 Verification Notes

## Standards and compatibility

- Primary flowchart reference: ISO 5807:1985 (published; confirmed current by ISO in 2019), for data/program/system flowchart symbols and use conventions.
- Historical sources: NIST-hosted FIPS PUB 24 / ANSI X3.5-1970 and IBM C20-8152 Flowcharting Techniques. They inform symbol meaning, direction and legibility, not a claim of current regulatory compliance.
- Do not reproduce copyrighted standards text, figures, scans, or IBM templates. The implementation uses independently authored marks and concise source attribution.
- Interchange compatibility is an evolving file-format family; pin upstream draw.io documentation revision and fixture provenance. Import must reject DTDs/external entities and remote resources.

## Review status

Implementation and automated validation are complete; five-reviewer usability acceptance is pending. Record focused/full test results, rendered image paths, viewing scale, layout issues, and reviewer ratings here. Do not mark visual acceptance complete before the actual review is conducted.

## Label-fit acceptance rebaseline

- User-requested label readability work is fixed as AC-018, FR-022, and SC-013. The criterion requires complete visible labels, deterministic wrapping, measured label bounds, collision avoidance, and an actionable pre-write failure when imported fixed geometry cannot fit its text.
- Acceptance criteria were intentionally refreshed to `2df21a66df3828aea1c0a8eb30ca188415e73cefe6befbe00121235515fbba0c` after adding that requirement; future changes must preserve this baseline unless reviewed again.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~LocalFlowchartTests` — passed 41/41 tests, including long node/edge labels, node/label/connector bounds, imported-size fit diagnostics, and tooltip details.
- BrownPants rendered `TestResults/label-review-top-down.png` locally at 1548 × 1592 pixels with a matching `.drawio` file. Full-resolution review showed complete node labels and separated edge labels; concise visible labels with longer tooltip details remain the recommended authoring style. This is a synthetic declared workflow, not a source-derived architecture chart.

## Tiled renderer check

- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~LocalFlowchartTests` — passed, 35/35 tests after adding caller-chart CLI integration coverage.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CallTree` — passed, 4/4 tests covering exact target selection, direct/transitive callers, source locations/depth, partial resolution, sampling omissions, and test-path classification.
- The caller-chart CLI integration fixture required explicit `--semantic`, loaded a local .NET 10 project, and wrote a parseable PNG plus editable `.drawio` with test-path candidates marked.
- BrownPants self-inspection loaded 12/12 managed projects and resolved 12,003 invocation targets. A caller chart for `LocalFlowchartLayoutEngine.Create` produced 12 direct callers; the report correctly marked 12 unresolved relationships and the depth-one limit. The first full-signature chart was too crowded; the compact call-site-node layout was visually reviewed at full resolution and is substantially clearer. Stakeholder review is still pending.
- A compact BrownPants self-chart for `LocalFlowchartPngRenderer.Render` shows two production callers and one test-path candidate at depth 1. Its 1,888 × 686 PNG and editable XML are published in `docs/flows/`; the three-sentence summary and README disclose the depth bound, unresolved relationships, and candidate-only test path.
- Regenerated `docs/flows/access-review.png` after the PNG compression update. Its decoded pixels matched the prior showcase, and the refreshed PNG and XML each reproduced byte-for-byte on a second local render.
- Caller node tooltips in editable XML retain exact compiler symbols and full source paths/lines, while PNG labels use compact method names and abbreviated paths. Non-ASCII labels no longer crash rendering; editable XML preserves their text, while the built-in ASCII PNG font substitutes `?` for unsupported glyphs.
- Generated an 80-node, 140-edge, two-dimensional scale fixture through `brownpants diagram` with `--tile-size 768 --overlap 96`. It produced 12 ordered tiles for a 2440 × 1488 canvas, paired `.drawio` sidecars, and a manifest. The first two tiles were visually inspected; shared connectors continue across their 96-pixel overlap. The fixture was generated in ignored `TestResults/` and is not a product example.
- Geometry and manifest tests verify every grid cell is present, tile bounds stay within the logical canvas, adjacent regions overlap by the requested amount, PNG/XML outputs are deterministic, and a sidecar collision prevents partial output.
- A 5,200-node sparse chain lays out to a 977,584-pixel canvas without rescanning all graph edges at each rank. A 64-node chart renders as one PNG wider than 4,096 pixels; scanlines are encoded incrementally, and the single-raster buffer uses a 10% reserve below .NET's maximum byte-array length.
- Automatic tile selection is covered at the single-raster boundary; lazy tile-region enumeration is verified through 1,025 regions. Tile PNG/XML generation stages one image at a time, and the manifest streams directly to disk. A separate managed stress run below verifies a complete bundle well beyond 1,024 tile pairs.
- A shared 1,024-pixel spatial index filters raster candidates per tile. On the 5,200-node chain fixture, a 256-pixel end tile selects fewer than 10 nodes and 12 edges instead of scanning/rasterizing the full graph.
- A tile-by-tile geometry regression independently calculates padded node, connector, and edge-label bounds for the representative flow; indexed candidates match every item that can affect each tile.
- PNG/XML sidecars are deterministic; a sidecar collision blocks partial PNG creation, and `--overwrite` replaces the image and editable XML together.
- The `--direction left-to-right` CLI option is verified against the actual PNG dimensions for the selected layout.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed, zero warnings/errors after caller-chart and Unicode handling changes. `dotnet test BrownPants.sln -c Release --no-restore` — passed, 319/319 tests.
- `acceptance-criteria-hash.ps1` verified the unchanged criteria digest `9b0181dcd3f7c85f0333ed955334f11cf7a6b10885b2abb08ab065a339d4f83f`; `check-acceptance-evidence.ps1` validated 42 task mappings and 29 criteria. The checker confirms structure; task-level test and review outcomes remain separately recorded.
- One-million-node end-to-end rendering and the five-reviewer usability requirement remain unverified. Caller charts are implemented with bounded compiler evidence; diagrams.net import now supports the documented subset and is not full-format compatibility.

## Current validation after label-fit changes

- `dotnet build BrownPants.sln -c Release --no-restore` — passed with zero warnings and errors after diagrams.net import work.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~LocalFlowchartTests` — passed, 45/45 tests.
- `dotnet test BrownPants.sln -c Release --no-restore` — passed, 328/328 tests.
- `acceptance-criteria-hash.ps1` verified `2df21a66df3828aea1c0a8eb30ca188415e73cefe6befbe00121235515fbba0c`.
- `check-acceptance-evidence.ps1` verified 43 task mappings and 31 criteria. An earlier run found T024 pointing to a directory; its evidence now points to the corpus README and a representative `.drawio` fixture.
- The documented import path ran against `docs/flows/access-review.drawio`: `dotnet run --project src/BrownPants.Cli -c Release --no-build -- diagram . docs/flows/access-review.drawio --input-format drawio --output TestResults/drawio-import.png` — created the PNG and editable XML sidecar and printed the evidence-limited plain-English summary.
- README quickstart scenarios for process flow, organization, brainstorm, and diagrams.net import all created PNGs and editable XML sidecars under ignored `TestResults/`.
- `git diff --check` passed; Git reported only existing LF-to-CRLF conversion notices for modified files.

## Current verification after grouped tile output

- The requested tile-folder behavior is AC-019/FR-023/SC-014. The reviewed and refreshed acceptance hash is `aa77acbbf2f19d5cc43f1ba39bb999a6e5c4af7de671dc99f7ab2ab9ccf139da`.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed with zero warnings and errors. Latest `dotnet test BrownPants.sln -c Release --no-restore` passed 334/334 tests; focused `LocalFlowchartTests` passed 51/51.
- Acceptance-hash validation passed at `f9a39e19f07e9c4b460fe63d77d529cbd33fe203e4e9daf310977f8ef8bb6048`; acceptance-evidence validation passed with 48 task mappings and 39 criteria.
- `dotnet restore BrownPants.sln --locked-mode` — passed for both projects. `dotnet list BrownPants.sln package --vulnerable --include-transitive` — reported no vulnerable packages for either project using the configured NuGet.org source on 2026-10-15.
- Roslyn caller CLI smoke run for `LocalFlowchartLayoutEngine.Create`, depth 3/sample limit 1,000, created 17 tiles at 4,000 pixels with 96-pixel overlap. The 63,480 × 1,960 canvas had 247 call-site nodes and 302 edges. All 35 artifacts (17 PNG, 17 XML, manifest) were placed under `TestResults/local-layout-callers-bundle/`; the manifest paths point inside that directory. Summary reported 16 direct callers, 79 transitive callers, 84 test-path candidates, and 13 unresolved relationships. Only the first tile was visually inspected; it is dense but legible at full tile resolution. Overall stakeholder usability remains unreviewed.

## Sparse-chain layout scaling

- Crossing reduction now exits before constructing rank-pair adjacency maps when every rank has at most one node; such a graph has no within-rank ordering choices and therefore no reducible crossings.
- `Diagram_LayoutsLongSparseGraphWithoutRepeatedWholeGraphScans` now lays out a 25,000-node / 24,999-edge chain and checks rank endpoints, canvas bounds, and spatial-index locality. Focused Release test passed: 1/1 test, 1.389 seconds test duration (2.081 seconds total test process).
- This validates the sparse layout path at 25,000 nodes. It does not establish one-million-node end-to-end PNG generation; that scale remains unverified.

## Connector-crossing diagnostics

- Added AC-020/SC-015 and refreshed the hashed acceptance baseline to `c829e32aa80076c4d0d3c427222480d307d93ab45a924fe1d75f674eb3f20e58`.
- Layout checks for orthogonal intersections between unrelated connectors using the spatial index and reports remaining crossing pairs in CLI output without changing graph edges. Above 20,000 edges it reports that analysis was skipped rather than implying that the diagram is crossing-free.
- Focused Release chart tests passed 48/48, including a known crossing, retained edge count, CLI diagnostic formatting, and the 20,001-edge analysis-bound case.
- A new dense graph regression initially exposed return connectors crossing same-rank nodes. Return edges now travel through rank-gap channels and dedicated outer lanes; long edges use the same obstacle-free rank-gap pattern. Top-down and left-to-right tests cover cycles, dense branches, disconnected paths, self-loops, parallel edges, and unequal path lengths, and assert that connectors avoid unrelated node interiors.
- Updated focused Release flowchart suite passed 50/50 after the routing fix and access-review ordering regression.
- Ranking now uses the majority direction of node declaration order as a stable forward-flow hint, then assigns longest-path ranks over that acyclic ordering; the remaining return edges keep their declared connections and use outer channels. The access-review regression verifies `register` precedes `risk`, `manual` precedes `grant`, and `retry` returns to `parse` while all connectors avoid unrelated shapes.
- CLI showcase smoke run of `docs/flows/access-review.md` wrote local PNG/XML into ignored `TestResults/`. The displayed chart reports two remaining crossings (`risk->grant` with `manual->retry`, and `manual->grant` with `retry->parse`). Full-size visual inspection found the main path readable, but the result still has two crossing pairs and is not yet a zero-crossing showcase.

## Streaming editable tiled sources

- T047 streams the editable diagrams.net XML template to staged storage and reuses the layout already computed for tile rendering; it avoids a second XML-sized byte array and a second full layout pass.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~DrawioWriter_StreamsTheSameDeterministicXmlAndLeavesDestinationOpen` — passed 1/1. The stream output is byte-identical to the byte-array writer and remains open for its caller. The full flowchart suite passed 51/51, the Release solution build passed with zero warnings/errors, and the full solution suite passed 334/334.
- This reduces peak memory during XML generation. Each tile still receives a full diagram sidecar, so total disk usage can scale with tile count; one-million-node end-to-end generation remains unverified.

## Native managed .NET rendering requirement

- User requirement: keep charting as close to native .NET as possible. AC-021/FR-024/SC-016 make this a hashed requirement. The accepted boundary is BrownPants-authored managed C# using .NET platform libraries for layout, rasterization, PNG encoding, and diagrams.net XML; no chart toolkit, platform-specific rendering API, external process, or network service.
- Refreshed the acceptance baseline to `52b49d9f46eb4144216d736a36b79c42bcfc43bb1a7c5e61ec08774e266d213e` to record the user's new requirement.
- Source audit found no chart toolkit, native rendering, network, or external-process API references in the flowchart/caller-chart pipeline. The CLI dependency graph includes no charting package; its Microsoft compiler/workspace dependencies serve repository analysis, not chart rendering. Release build and all 334 tests passed.

## Chart quality rubric

- User's review standard is “simple, elegant, usable, useful.” AC-002/AC-022 and SC-017 now define each dimension, require five reviewer scores for compact and complex charts, and expect at least four reviewers to score each dimension at least 4/5 and identify the purpose and takeaway.
- The acceptance baseline was refreshed to `f9a39e19f07e9c4b460fe63d77d529cbd33fe203e4e9daf310977f8ef8bb6048`. T012/T029 remain open until actual stakeholder review and scores are recorded; visual inspection by the implementer is not a substitute.

## Source-chart QA focused on usefulness

- User direction: chart usefulness takes priority over proving a million-node end-to-end limit. The parser's upper input limits remain safety boundaries, not a target for release benchmarking; the existing 25,000-node sparse layout test is sufficient stress coverage for this pass. Prefer compact charts that answer a concrete QA question.
- `./scripts/qa-chart-engine.ps1` compares a real caller chart for `LocalFlowchartDrawioWriter.Write` against a separate BrownPants `impact --format json` run. It matched 4 caller methods, 5 source call-site path/line entries, and 10 XML edges exactly. The chart output clearly disclosed 13 unresolved delegate targets and marked coverage partial.
- The comparison checks BrownPants' chart conversion against its separately emitted impact report; both use the same Roslyn semantic analyzer, so this is a data-integrity check, not an independent compiler oracle. Analyzer correctness remains covered by expected-result syntax/semantic fixtures.
- The first candidate, `LocalFlowchartLayoutEngine.Create`, had 21 caller methods and 23 call sites. Although the independent report matched, its chart was too tall and dense to be useful in a routine QA review. The repeatable script uses the smaller writer target; this is an explicit usefulness choice, not a claim that dense charts are inaccurate.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed, zero warnings/errors. `dotnet test BrownPants.sln -c Release --no-restore` — passed, 336/336 tests.
- Source caller charts answer “who calls this method?” They do not explain what the file or method does internally. Detailed behavior charts must represent decisions and outcomes such as `if`/`else`, switch cases, loops, early returns, and exceptions with source evidence. That capability is recorded as idea queue item 32 and is not claimed by this caller-chart feature.
- The acceptance baseline was refreshed to the reviewed AC-024/SC-019 wording for independent call-impact comparison; no numeric scale target was added.

## Agent and human feature review

The reusable questions in `docs/feature-review-checklist.md` were applied to the chart capabilities:

| Capability | Agents can use it | People can use it | Useful job | Meaningful next improvement |
|---|---|---|---|---|
| Process, organization, and brainstorm diagrams | Yes: `diagram` is documented in CLI help and agent context instructions. | Yes: README examples open as PNG and editable diagrams.net files; input and commands are local. | Explain a declared workflow, reporting tree, or nested idea map. | Improve remaining connector crossings and add user-selected themes; track theme support in queue item 26. |
| Caller and impact charts | Yes: `chart callers` maps an exact compiler symbol to source-backed edges. | Yes: local CLI creates PNG and editable XML that can be opened and reviewed. | Find affected callers and QA test-path candidates for a code change. | Reduce exact-symbol discovery friction and extend the evidence bundle for bug research; queue item 34. |
| Method behavior charts | Yes: `chart flow` is documented for C# and VB and invoked by an agent with an exact symbol. | Yes: PNG, tiled output, and editable XML can be opened without an AI host. | Follow a method's decisions, loops, returns, and explicit exception paths. | Improve the observed crossing-heavy CFG layouts; preserve the explicit boundary that implicit runtime exceptions are not modeled. |
| Large/tiled output | Yes: commands and manifests are machine-readable and deterministic. | Yes: users can inspect ordered PNG tiles and their editable sidecars. | Review a graph that exceeds a practical single image size. | Add a friendlier tile index or overview if real users find manifest navigation cumbersome. |
| diagrams.net import/export | Yes: `diagram --input-format drawio` supports the documented corpus. | Yes: editable sidecars and imported PNGs are directly reviewable. | Continue editing supported diagrams in diagrams.net. | Expand compatibility only from real files and explicit fidelity tests; current scope is a tested subset, not 100% compatibility. |

All chart features have CLI and README/agent entry points, but they do not yet provide a graphical chart-authoring interface. People can open outputs in common viewers/editors; input authoring and source analysis remain CLI-driven. C# and VB CLI smoke tests produced PNG/XML successfully, and repeated C# PNG/XML output hashes matched. The behavior chart reported two connector crossings for the representative loop/throw method; it is truthful and complete, but that layout remains a release-candidate review item rather than a polished showcase claim.

## Compact and tiled examples

- Added a compact release-check fixture and a 28-step release-pipeline fixture in `tests/BrownPants.Tests/Fixtures/LocalFlowcharts/`. The compact chart renders at 1188 × 1208 pixels; at native size, all labels fit their shapes, node and edge-label bounds do not overlap, and no label is truncated. The PNG and editable XML are checked in under `docs/flows/`.
- The release pipeline rendered from the checked-in Markdown with `--tile-size 1800 --overlap 128`. Its 1684 × 5480 canvas is covered by four row-major tiles; the final tile ends at the canvas boundary. The manifest, all PNG dimensions, each editable sidecar, and the 128-pixel adjacent overlaps were checked. All four images were visually reviewed in manifest order.
- The large renderer output discloses four remaining connector crossings where retry/recovery paths return upstream. The full declared connections are retained. This is recorded as a known layout limitation, not a zero-crossing result.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Diagram_CompactReleaseCheck|FullyQualifiedName~Diagram_ReleasePipelineShowcase'` — passed, 2/2. `dotnet build BrownPants.sln -c Release --no-restore` — passed with zero warnings/errors. `dotnet test BrownPants.sln -c Release --no-restore` — passed, 344/344.
- Regenerated the compact image, sidecar, four large tiles, four sidecars, and manifest from the documented CLI commands. All 11 output SHA-256 hashes matched the prior generation.
- README now defines brownfield and SDD for new users, explains why repository evidence matters before writing a change spec, and walks through inspection, impact research, acceptance criteria, implementation, and QA verification. It clarifies that BrownPants supplies evidence while teams decide requirements.
- T033 and T035 are complete. T012/T029 remain open pending five real stakeholder reviews; implementer visual inspection is not treated as a substitute. Remaining method-CFG verification is tracked separately in T057.

## C# and Visual Basic method behavior verification

- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~MethodControlFlowDiagramTests` — passed, 6/6. Fixtures compare every modeled C# and VB block and successor, true/false branches, return and explicit exception semantics, source lines and editable XML targets; both language paths compare deterministic PNG/XML bytes. Tests also cover VB SDK-project loading, incomplete CFG rejection, `--semantic` opt-in, and repository path traversal diagnostics.
- BrownPants generated the compact self-analysis example with `chart flow` for `LocalFlowchartCommand.ResolveInside`: 17 Roslyn CFG blocks, five decisions, and two explicit throw/rethrow paths. The local PNG and editable XML are in `docs/flows/brownpants-path-safety.*`; implicit runtime exceptions are explicitly excluded.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed with zero warnings/errors. `dotnet test BrownPants.sln -c Release --no-restore` — passed, 344/344. The frozen acceptance digest verified as `fdd5825c138ae898b514206ec577b1b411ef226dd62a93ad219d1de88f06e452`, and the evidence checker accepted all 57 task mappings and 46 criteria.
- T057 is complete. T012 and T029 remain open for stakeholder ratings; T057's implementation and automated verification do not replace the required human usability review.

## Arrowhead visibility across node shapes

- Visual QA found that node fills were drawn over connector arrowheads, and the triangle rasterizer omitted its tip pixel. PNG rendering now draws nodes before arrowheads and writes each arrow tip explicitly. Arrowheads use the same connector color as their lines.
- `Diagram_ArrowheadTipsRemainVisibleAtTargetShapePerimeter` checks the tip pixel for all seven supported target shapes: start, process, decision, input/output, person, cloud, and end.
- BrownPants-authored charts now apply the same default palette to PNG and editable XML. Default decision and shape colors match between outputs; imported XML keeps its original colors unless a theme is selected.
- Regenerated the checked-in access-review and blue-water charts, compact review flow, team ownership, brainstorm, Roslyn call-resolution, BrownPants path-safety and caller charts, approval sample, and tiled release pipeline using the current renderer. The caller-chart guide now records the updated 3-parameter symbol, five direct calls, three test-path candidates, and 13 unresolved delegate targets.
- `pwsh -NoProfile -File scripts/qa-chart-engine.ps1` — passed after updating its exact writer symbol for the theme parameter. BrownPants' caller chart matched the independent impact report: 4 direct callers, 5 source call sites, and 10 editable-XML edges; 13 unresolved delegate targets remained disclosed. The cleanup hook preserved 496 unmanaged legacy `TestResults` entries for manual review.
- Cross-platform check with the cached Microsoft .NET 10 SDK container: `dotnet restore BrownPants.sln --locked-mode`, Release build, and full test suite passed on Linux (368/368). A Linux-rendered compact chart matched the checked-in Windows PNG and `.drawio` SHA-256 hashes exactly: PNG `9847356ADBE971A6B1B24464A9B82F0437CEF61E581313DB3CACD6CCE924918D`; XML `7FDCD684CCBACC453ABE30BC67C35BBFEB5337678FB09E6BC26B47071B2593C8`.
- `dotnet test BrownPants.sln -c Release --no-restore` — passed, 368/368. The focused arrowhead and theme suite passed, 23/23. Release build passed with zero warnings/errors; the configured NuGet source reported no vulnerable packages.
- The acceptance baseline now includes AC-027 and SC-021, digest `9a02495835bbe110242d9b8a9d404f649b07763bc363605a46aa63416c118679`. T058 is complete. T012/T029 remain open for the five real stakeholder chart reviews.

## End-to-end tile bundle beyond 1,024 tiles

- Generated a 2,200-node / 2,199-edge chain and rendered it with `--tile-size 256 --overlap 64`. The 296 × 413,584 logical canvas produced 4,308 ordered PNG tiles and 4,308 editable XML sidecars.
- Validated every manifest image and XML path against a nonempty file, checked the manifest and on-disk counts, and marked the generated run successful. No tile-count limit interrupted rendering.
- Managed run: `TestResults/.brownpants/runs/20261016T041241Z-chart-over-1024-tiles-977e277e4307400ba6b0298320312969/`. The run is disposable test output and follows the existing managed retention policy.
- This verifies streaming and bundle completion at several times the former 1,024-tile boundary. It does not establish one-million-node end-to-end rendering; that scale remains unverified.

## Crossing-aware back-edge routing

- Review of the release-pipeline showcase showed that safe back-edge routes always preferred the right lane, even when it crossed a forward connector. Back-edge routing now compares the node-safe left/right routes against already placed unrelated connectors and selects the route with fewer crossings, using path length as the tie-breaker.
- On the 28-node/34-edge release-pipeline fixture, the existing crossing diagnostic fell from seven pairs to four. The test caps the result at four, keeping the observed reduction as a regression check. The remaining four paths are disclosed in the README and example notes; zero crossings are not claimed.
- The current CLI run reports three sampled crossing locations: `unitFix->unit` with `unitOk->full` at canvas `(1033, 1616)`, `fullFix->full` with `fullOk->audit` at `(1041, 2156)`, and `smokeFix->package` with `smokeOk->notes` at `(1001, 3488)`. The count is four pairs; diagnostics intentionally cap displayed examples at three.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~LocalFlowchartTests` — passed, 54/54. `dotnet build BrownPants.sln -c Release --no-restore` — passed with zero warnings/errors. `dotnet test BrownPants.sln -c Release --no-restore` — passed, 344/344.
- `pwsh -NoProfile -File scripts/qa-chart-engine.ps1` — passed: the BrownPants caller chart matched a separately emitted Roslyn impact report for the target, four callers, five source call sites, and ten XML edges; 13 unresolved delegate relationships remained explicitly disclosed.
- Crossing diagnostics now include the canvas intersection point. The four updated release-pipeline tiles were regenerated and the first tile visually reviewed. The remaining crossings still need layout work and stakeholder review.
- Added `docs/chart-review-scorecard.md` and linked it from README. It captures independent scores for both chart sizes, asks for a 30-second purpose/takeaway, and states the existing five-reviewer acceptance thresholds without inventing results.
- Follow-up routing work checks side-entry alternatives for adjacent forward edges when their direct route would cross an already routed connector. On the same 28-node/34-edge release-pipeline fixture, this reduced the measured crossings from four to two: `unitFix->unit` with `unitOk->full` at `(1033, 1616)`, and `fullFix->full` with `fullOk->audit` at `(1041, 2156)`. A trial that routed forward edges ahead of back-edges raised the count to seven and was reverted.
- Tightened `Diagram_ReleasePipelineShowcaseHasOrderedTilesAndContinuityOverlap` to require no more than two unrelated crossings. The focused release-pipeline and crossing-diagnostic tests pass (2/2). Regenerated all four checked-in tiles and reviewed tile 1; the remaining two crossings are disclosed in the README and source note.
- Follow-up Release validation: `dotnet build BrownPants.sln -c Release --no-restore` passed with zero warnings/errors; `dotnet test BrownPants.sln -c Release --no-restore` passed 344/344. `pwsh -NoProfile -File scripts/qa-chart-engine.ps1` matched the caller chart against independent Roslyn impact output (4 callers, 5 call sites, 10 XML edges; 13 unresolved delegate targets disclosed). The acceptance hash and evidence checker passed (57 tasks, 46 criteria), `dotnet list BrownPants.sln package --vulnerable --include-transitive` reported no vulnerable packages, and `git diff --check` reported no whitespace errors. T012/T029 still require five real stakeholder usability reviews; these automated and implementer checks do not satisfy that gate.

## Tile-local editable sources

- The first 2,200-node stress run confirmed a storage defect: each of 4,308 tile sidecars repeated the entire 1.73 MB graph, totaling 7,451,293,428 XML bytes.
- Feature 071 changes tiled bundles to store one complete master and standalone viewport XML beside each image. Repeating the same 2,200-node render now produces the same 4,308 tiles with 8,818,944 XML bytes total (8.41 MiB); all sidecars import through BrownPants' parser, all manifest paths resolve, and SHA-256 checks over all 8,618 artifacts match across repeat runs.
- The standalone tile views preserve route/label intersections and endpoint cells, including bounded off-page coordinates. Their size and interchange contract is documented in [the manifest contract](../071-tile-local-editable-sources/contracts/tile-bundle-manifest.md); test and package evidence is recorded in [Feature 071 verification](../071-tile-local-editable-sources/verification.md).

## Completed-route crossing refinement

- Added a bounded second pass for top-down layouts with at most 2,000 edges. It rechecks adjacent forward and return-edge routes against the completed graph, considers deterministic alternatives in the reserved side lanes, and rejects candidates that intersect unrelated nodes or increase the route's unrelated crossing count. Declared edge endpoints and labels remain unchanged.
- The release-pipeline fixture now has zero unrelated connector crossings while retaining all 28 nodes and 34 connections. `Diagram_ReleasePipelineShowcaseHasOrderedTilesAndContinuityOverlap` now requires zero crossings, and all four PNG tiles plus editable tile sidecars and the master XML were regenerated.
- QA-roleplay review caught that rerouted edges retained label positions from their former paths. Label placement now runs after route refinement, and the showcase regression checks that labels remain in bounds and clear of nodes, other labels, and unrelated connectors. The regenerated tile outputs include these final label positions.
- Visual review of the first two 1800-pixel tiles confirmed the upstream paths remain visible across the shared tile boundary. This is implementer review only and does not satisfy the five-stakeholder usability gate in T012/T029.
- `dotnet build BrownPants.sln -c Release --no-restore` passed with zero warnings/errors; `dotnet test BrownPants.sln -c Release --no-build` passed 371/371. `pwsh -NoProfile -File scripts/qa-chart-engine.ps1` matched the writer caller chart against independent Roslyn impact output: 5 callers, 6 call sites, and 12 XML edges; 14 unresolved delegate targets remain disclosed. Legacy unmarked `TestResults` entries were preserved by cleanup.
