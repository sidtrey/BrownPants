# Implementation Plan: OpenSpec BrownPants Context Adapter

**Branch**: `003-openspec-context-adapter` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Add a manually invoked Codex skill for OpenSpec users plus an idempotent PowerShell installer. Extend the shared inventory with engine-neutral SDD workflow records that identify Spec Kit and OpenSpec artifacts, current specifications, and active changes. The adapter runs this report, then directs the agent to the active OpenSpec change and relevant source files. Keep engine-specific instructions in `integrations/openspec/`; do not modify OpenSpec-generated skills.

## Technical Context

**Language/Version**: PowerShell 7-compatible script, Markdown skill instructions, .NET 10 BrownPants CLI
**Primary Dependencies**: Existing `brownpants` tool; OpenSpec CLI and Codex skill discovery
**Storage**: Filesystem only; inventory reports workflow artifact metadata and installs one skill under `.agents/skills/openspec-brownpants-context/`
**Testing**: Existing xUnit suite plus disposable OpenSpec project installation and Codex skill smoke test
**Target Platform**: Windows, macOS, and Linux repositories using Codex CLI
**Project Type**: Repository CLI plus SDD integration
**Performance Goals**: Installer completes without network access; context scan uses existing CLI defaults
**Constraints**: Never replace an existing different skill; no automatic hook because OpenSpec exposes no Spec Kit-style lifecycle hook in this integration; cap reported artifact paths by the existing sample limit
**Scale/Scope**: Engine-neutral workflow inventory for Spec Kit/OpenSpec, one Codex skill, one cross-platform PowerShell installer, integration docs, and fixture-level verification

## Constitution Check

- Deterministic evidence and explicit analysis limits: pass; reuse BrownPants Markdown output.
- Safe repository changes: pass; installer creates one new directory and refuses conflicting content.
- Tests: pass; verify idempotency and conflict handling in a disposable project plus existing xUnit suite.
- SDD workflow neutrality: pass; workflow-specific discovery is represented with a generic record and adapter instructions remain separate.

## Project Structure

```text
integrations/openspec/
├── README.md
├── Install.ps1
└── skills/openspec-brownpants-context/SKILL.md
specs/003-openspec-context-adapter/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/context-skill.md
└── tasks.md
```

**Structure Decision**: Keep the adapter alongside the existing Spec Kit integration and install its Codex skill into a separate name under `.agents/skills/`. Use a shared SDD workflow analyzer and generic inventory record rather than adding OpenSpec fields to the repository root model. Tests use temporary repositories to verify engine discovery, output bounds, and installer behavior.

## Complexity Tracking

No constitution violations or additional runtime dependencies.
