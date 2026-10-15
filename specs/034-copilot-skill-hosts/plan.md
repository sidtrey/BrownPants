# Implementation Plan: BrownPants GUI Skill for GitHub Copilot

**Branch**: `034-copilot-skill-hosts` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Extend the existing offline project-skill installer with a GitHub Copilot target and an `all` target. Keep the canonical skill content shared; add host-specific installation guidance and tests for Copilot-only installation and atomic multi-target preflight.

## Technical Context

**Language/Version**: PowerShell 7; Markdown
**Primary Dependencies**: PowerShell built-ins only
**Storage**: Repository files
**Testing**: `tests/AgentSkillIntegrationTests.ps1`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Integration adapter
**Performance Goals**: Not applicable; installer handles three local files
**Constraints**: No downloads, network calls, AI-host CLI, or overwriting user files; all writes remain within the selected project
**Scale/Scope**: One canonical skill copied to Codex, Claude Code, and Copilot-compatible workspace locations

## Constitution Check

- Engine-agnostic core: pass; changes are isolated to an adapter.
- Safe inspection and least surprise: pass; installer uses local files and preflights every destination before writes.
- Specified and verified changes: pass; fixture-based installer tests and host-limit documentation are required.
- No new dependencies or changes to scanner evidence: pass.

## Project Structure

```text
integrations/agent-skills/Install.ps1
integrations/agent-skills/README.md
integrations/agent-skills/brownpants-context/SKILL.md
integrations/copilot/README.md
integrations/README.md
tests/AgentSkillIntegrationTests.ps1
specs/034-copilot-skill-hosts/
```

**Structure Decision**: Extend the existing shared agent-skill installer rather than duplicate a second skill or create another adapter. The Copilot prompt adapter remains available for Visual Studio 2022 and VS Code Local harness.

## Design

Add `copilot` and `all` as installer selections; preserve `codex`, `claude`, and `both`. The Copilot destination is `.github/skills/brownpants-context/SKILL.md`. The installer already preflights target definitions as a set, so the new selection must flow through that existing mechanism. The test suite will verify exact file identity, rerun stability, conflict preservation, and no partial writes when any `all` target conflicts.

Document VS Code Copilot harness skill support and Visual Studio 2026 version requirements from vendor documentation. Keep the prompt path for Visual Studio 2022 and VS Code Local harness, because prompt-file support differs by host/harness. Explicitly state that discovery does not guarantee activation and that the skill respects terminal and organization restrictions.

## Complexity Tracking

No constitution violations. No production packages, network configuration, or AI client are added.
