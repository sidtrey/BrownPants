# Quickstart: Tile-Local Editable Sources

1. Generate the checked-in release-pipeline example into a temporary repository-local output folder:

   ```powershell
   dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/tiled-release-pipeline.md --output TestResults/tile-review/pipeline.png --tile-size 1800 --overlap 128 --overwrite
   ```

2. Confirm `TestResults/tile-review/pipeline/pipeline.tiles.json` includes root `editableSource` for the complete graph and ordered per-tile `image`/`editableSource` paths.
3. Open `TestResults/tile-review/pipeline/pipeline.drawio` to edit the complete chart. Open one tile PNG and its adjacent `.drawio` file to inspect that viewport alone. Tile XML uses tile-local coordinates, includes intersecting nodes and routes plus valid edge terminals, and may retain endpoint cells just outside the image.
4. Open the bundle's complete `.drawio` source and confirm all source nodes and edges are present.
5. Repeat the command and compare PNG, tile XML, complete XML, and manifest SHA-256 hashes. Run `./scripts/test.ps1` for the repository suite.

The large acceptance fixture uses 2,200 nodes, 2,199 edges, `--tile-size 256`, and `--overlap 64`; it must produce more than 1,024 tile pairs while keeping total XML output at or below 50 MB.
