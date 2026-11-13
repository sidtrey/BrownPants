# Quickstart: Validate Local Flowcharts

1. Build and run focused tests:

   ```powershell
   dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj --filter FullyQualifiedName~LocalFlowchartTests
   ```

2. Render a fixture from the repository root:

   ```powershell
   dotnet run --project src/BrownPants.Cli -- diagram . docs/flow.md --output artifacts/flow.png --overwrite
   ```

   Charts larger than the single-image raster budget are tiled automatically at 4,000 pixels per tile. To choose the tile dimensions, add `--tile-size 1024 --overlap 96`. All tiles, editable `.drawio` sidecars, and the streamed `<name>.tiles.json` manifest are grouped under a `<name>/` subfolder beside the requested output path. Tile names include their canvas pixel origins; review them by manifest order and use its shared-edge overlap when moving between adjacent images.

3. Render the same input and options twice. Confirm deterministic PNG bytes, tile/manifest bytes when tiled output is requested, and XML bytes.
   Tiled bundles live under a subfolder named for the output stem (for example, `artifacts/flow/flow.tiles.json`).
4. Check geometry fixtures for overlaps, edge/node intersections, crossings, clipping, and complete node/edge representation.
5. Render dense-branch, long-label, cycle, disconnected-component, and multi-tile examples. Inspect full-resolution outputs and every tile at the documented viewing scale.
6. Validate every compatibility-corpus sample by round-trip or an explicit unsupported-construct diagnostic.
7. Verify the same supported examples with network access disabled; output must remain local and complete.

## Source-backed caller graph

On the managed fixture, first find the fully qualified `ImportOne` declaration symbol with semantic inspection, then map “everything that calls this method” to the caller command. Use the exact symbol returned by inspection:

```powershell
brownpants inspect . --semantic --format markdown --limit 10000
brownpants chart callers . --source examples/brownfield-bug-lab/managed/Workflows/OrderImportWorkflow.cs --method '<exact-fully-qualified-symbol>' --output TestResults/import-one-callers.png --format png --semantic
```

Confirm the graph includes only compiler-resolved call edges with source locations/depth, and visibly reports incomplete project coverage or sampling. QA test-path entries must be labeled candidates, never runtime coverage.

## Source-backed method behavior

To answer “what does this method do?”, use the exact declaration symbol from semantic inspection with `chart flow`:

```powershell
brownpants chart flow . --source src/Workflows/OrderImportWorkflow.cs --method '<exact-fully-qualified-symbol>' --semantic --output TestResults/import-one-flow.png
```

The chart represents Roslyn basic blocks, conditions, successors, loops, and explicit return/throw semantics; full operations and source locations are retained in the `.drawio` tooltips. It does not represent implicit runtime exceptions. If a complex method tiles, follow the tile manifest in row-major order.

This works for C# and Visual Basic projects. Use the compiler symbol exactly as printed by `inspect --semantic`; for a Visual Basic declaration, that symbol uses VB's `Global.Namespace.Type.Method(Integer)` display form. Project evaluation can execute imported MSBuild targets, so run semantic analysis only for trusted projects and when policy allows it.
