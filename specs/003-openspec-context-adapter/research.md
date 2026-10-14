# Research: OpenSpec BrownPants Context Adapter

## OpenSpec's Codex integration

OpenSpec initializes tool-specific skill files under `.agents/skills/openspec-*` for Codex. Its core workflow includes `propose`, `explore`, `apply`, `update`, `sync`, and `archive`; Codex invocation uses `$openspec-propose` and `$openspec-apply-change`. The proposed adapter installs a separately named skill and does not edit generated OpenSpec files.

## OpenSpec artifacts

The standard project source-of-truth specs live under `openspec/specs/`. Proposed work lives under `openspec/changes/<change>/`, commonly with `proposal.md`, `design.md`, `tasks.md`, and delta specs under `specs/`. The skill must inspect actual paths because custom schemas and incomplete changes may omit standard files.

## Lifecycle integration

OpenSpec does not expose the Spec Kit extension hook mechanism used by `integrations/speckit/`. Therefore this integration is invoked explicitly as `$openspec-brownpants-context` before propose/apply work. Do not claim automatic execution.

## Installation behavior

The installer copies only the bundled skill to `.agents/skills/openspec-brownpants-context/SKILL.md`. If the destination is absent it installs; if byte-identical it succeeds without rewriting; if different it fails with an actionable message. The installer does not invoke `openspec` or access the network.

## Evidence scope

Use `brownpants inspect . --format markdown` first and the `.tools` shim fallback. Treat reports as navigation evidence: candidate patterns need source verification, inventory-only language rows are not parsed, and no project is asserted buildable from manifest detection alone.

The OpenSpec layout uses `openspec/specs/` for current capabilities and `openspec/changes/<name>/` for proposed work; completed items move under `changes/archive/`. The shared report can expose those directory counts without depending on `proposal.md`, `design.md`, or `tasks.md` being present, which permits customized schemas.
