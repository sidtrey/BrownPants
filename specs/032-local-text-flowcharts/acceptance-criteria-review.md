# Acceptance Criteria Review

## Maintainer-directed scope update

**Previous digest**: `5d154f00a5bb61994a7a833ee149b592ad3ed386cb8d203611469d51dc3c26e2`

**Proposed digest**: `4c86f0cca3fc48b680a6139b2fbce175a00bea050ef006ee0e69fb71229cdfa2`.

**Reason**: The maintainer steered the queued charting feature toward a self-built .NET Mermaid renderer that produces PNG flowcharts. This replaces terminal-only ASCII output with a locally generated image while preserving the security goal: no hosted renderer or network access.

**Changed acceptance IDs**: AC-001 through AC-005; FR-001 through FR-008; SC-001 through SC-004. The new AC-005 makes output overwrite behavior explicit.

**Review evidence**: User direction in the active conversation: “maybe we could make our own .net mermaid rendering system? like it outputs png's of the flows”. The maintainer is directing the feature scope; implementation must use a documented Mermaid subset and retain path, size, and output-safety limits.

Only the repository maintainer may apply the `acceptance-criteria-reviewed` pull request label.

## diagrams.net import interoperability update

**Previous digest**: `4af5cf9608d1e820b97268b39180247397da67870dd36d9e44087cd07a080d74`.

**Proposed digest**: `41a11f4aacc437c294b83ef22a2dd1f96c0e57c73ca65641bd69ccb10df113ab`.

**Reason**: The maintainer asked whether generated diagrams could be imported into draw.io. BrownPants will optionally emit native uncompressed `.drawio` XML from its own flow DSL, in addition to its default PNG output. The export must contain only escaped text and diagram geometry, without scripts or remote references.

**Changed acceptance IDs**: AC-001, AC-006, FR-001, FR-003, FR-009, SC-005.

**Review evidence**: Official diagrams.net documentation lists `.drawio` XML as a native/importable format: https://www.drawio.com/docs/manual/import/import-formats/ and https://www.drawio.com/docs/manual/editor/save-file-formats/. The user's direction is “maybe something draw.io can import?”

Only the repository maintainer may apply the `acceptance-criteria-reviewed` pull request label.

## Security-format scope update

**Previous digest**: `4c86f0cca3fc48b680a6139b2fbce175a00bea050ef006ee0e69fb71229cdfa2`.

**Proposed digest**: `4af5cf9608d1e820b97268b39180247397da67870dd36d9e44087cd07a080d74`.

**Reason**: The maintainer clarified that organizations in security-sensitive environments may prohibit Mermaid tooling because of its AI/service association. BrownPants will therefore define and parse its own small `brownpants-flow` format; it will not accept Mermaid syntax or depend on Mermaid code, packages, services, or renderers. The requested output remains locally generated PNG.

**Changed acceptance IDs**: AC-001, FR-001, FR-002. Other PNG, safety, and offline requirements remain unchanged.

**Review evidence**: User direction: “as many companies in security areas cannot use the actual meraid chart,as mermaid uses AI, and its not secure for banking etc”. This instruction supersedes the previous Mermaid-subset proposal.

Only the repository maintainer may apply the `acceptance-criteria-reviewed` pull request label.
