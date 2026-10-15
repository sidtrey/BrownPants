# Implementation Plan: Cursor BrownPants Context Command

**Branch**: `037-cursor-context-command` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Add a Cursor-specific project slash command and conflict-safe installer. The command requests the same bounded local BrownPants evidence used by other adapters and explains an approved-shell/report fallback when terminal execution is unavailable.

## Technical Context

**Language/Version**: PowerShell 7-compatible installer/tests; Markdown command  
**Dependencies**: Existing PowerShell/.NET runtime only; no package  
**Storage**: One bundled Markdown source and one destination under `.cursor/commands/`  
**Testing**: PowerShell temporary-project integration test; full .NET build/xUnit suite  
**Constraints**: Local-only copy, no downloads, no AI CLI/SDK, no host-policy bypass, no unsafe overwrite or path escape.

## Constitution Check

- Evidence first: pass; the command asks for inventory and source verification, not unsupported conclusions.
- Broad host support: pass; add a host adapter without changing scanner semantics.
- Safe by default: pass; the command does not install BrownPants, access network, run semantic MSBuild, or modify repository source.
- Deterministic and reviewable: pass; one version-controlled Markdown command with a stable destination.
- Verified changes: pass; fixture tests cover install safety and prompt contract.

## Design

Place the user-facing command at `integrations/cursor/commands/brownpants-context.md`. Install it using `integrations/cursor/Install.ps1 -ProjectRoot <repo>` into `.cursor/commands/brownpants-context.md`. Match the established Copilot installer safeguards for existing project roots, identical content, conflicts, and reparse-point containment. Add `tests/CursorCommandIntegrationTests.ps1` and run it from CI. Document installation, invocation, terminal limits, and the lack of live GUI verification in `integrations/cursor/README.md` and the root README.

## Risks

Cursor currently describes custom commands as beta. Repository tests can validate the file format and installer but cannot validate activation inside a signed-in Cursor session; documentation must state that limit.
