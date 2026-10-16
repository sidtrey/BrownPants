# Tile Bundle Manifest Contract

The existing schema version remains `1`; fields are additive.

- Root `editableSource`: repository-relative path to the complete graph's `.drawio` file in the same tile bundle.
- Each `tiles[]` entry retains `image`, `editableSource`, order, row/column, pixel origin and dimensions, and overlap fields. Its sidecar contains a standalone viewport excerpt rather than the entire graph.
- Every entry path must remain within the dedicated bundle directory and point to a generated file. The root `editableSource` must also remain within that directory and use the bundle's base name.
- The complete source contains the full graph once. Tile excerpts contain intersecting graph elements and required endpoint cells, in original stable cell order, translated by the tile's `(x,y)` origin.
- Unknown root fields remain tolerated when reading an older valid manifest. Existing manifests without root `editableSource` are read as legacy bundles; overwrite replaces their tile paths and does not assume the old master file exists.
- New outputs are staged and committed together. A conflict or failed write leaves prior outputs and unrelated files unchanged.
