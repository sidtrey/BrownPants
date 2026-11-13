# Acceptance Criteria Baseline Review

- **Previous draft digest**: `67d5a782bde511dcedd863675f5d75c5962c190e0f03c96afad5635f4a92bce6`
- **Intermediate draft digest**: `f974aa66663a4ba613126540030282f4e797a9f3f597d9ef7446e101b9ed4e21`
- **Reviewed baseline digest**: `a72bd5ca7daff3a0eed1ae55538f3ea9d8fa79a1521ab96b6db2bd82efe413a0`
- **Reason**: Before implementation began, made each scenario traceable with an AC ID and explicitly required unthemed PNG/XML byte compatibility. This turns the requirements into a verifiable task ledger without changing the intended behavior.
- **Changed acceptance IDs**: AC-001 through AC-006, SC-002, and SC-007.
- **Review boundary**: This is a pre-implementation clarification of feature 070's initial draft. Any further acceptance-criteria edits must be reviewed and recorded separately before the baseline is refreshed.

## Renderer compatibility clarification

- **Previous reviewed digest**: `a72bd5ca7daff3a0eed1ae55538f3ea9d8fa79a1521ab96b6db2bd82efe413a0`
- **Refreshed digest**: `8b30d793e140dd3ff5192bf034aac6e171cef010ac47396a40e4bdd8c00a8eee`
- **Change**: Replaced the pre-theme byte-preservation requirement with parity between omitted theme selection and explicit `--theme default`.
- **Reason**: The chart renderer had a confirmed arrowhead visibility defect, and its existing default raster/XML palettes disagreed. Keeping a historical byte hash would preserve a broken image and inconsistent sidecar. The default palette is now applied consistently to BrownPants-authored source chart outputs; imported XML styles remain unchanged unless a theme is explicitly selected. Repeated output remains deterministic.
- **Review boundary**: This intentionally permits renderer correctness fixes to update historical bytes. It does not relax stable repeat output, graph/layout invariance under theme changes, or local managed rendering requirements.
