# Implementation Plan: Local Agent Context Skills

**Branch**: `029-local-agent-context-skills` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/029-local-agent-context-skills/spec.md`

## Summary

Publish one canonical, host-neutral BrownPants context skill and a local installer that copies it into Codex or Claude Code project skill locations. Keep the operation user-invoked, offline, evidence-limited, and compatible with a user-run report fallback when agent terminal access is restricted.

## Technical Context

**Language/Version**: PowerShell 7 for installer and integration tests; Markdown for skill and documentation  
**Primary Dependencies**: PowerShell/.NET filesystem APIs; no new packages  
**Storage**: Project-local skill files only  
**Testing**: PowerShell integration checks in temporary project roots; existing solution build and xUnit suite  
**Target Platform**: Windows, Linux, macOS; local Codex and Claude Code hosts  
**Project Type**: Repository integration adapter  
**Constraints**: No network calls, host CLI/SDK calls, project evaluation, source writes, or host policy bypass; preserve conflicting files; contain writes within the selected project  
**Scale/Scope**: One shared skill installed at one or both host-specific project locations

## Constitution Check

- Evidence First: Skill instructions separate observed inventory from lexical/syntax candidates and require source verification.
- Broad, Engine-Neutral Core: No scanner or report format changes; the skill works against mixed repositories.
- Deterministic and Reviewable Output: Installer uses fixed source and destination names and reports relative destinations.
- Safe Inspection by Default: Workflow asks for read-only Markdown inventory without semantic MSBuild evaluation; installer validates containment and never accesses the network.
- Specified and Verified Changes: Host contract checks and isolated temporary-root tests cover all acceptance criteria.

## Research Decisions

See [research.md](research.md). Codex and Claude Code support project-local `SKILL.md` workflows, but discovery, invocation, and policy behavior are host-specific. Use their project locations and document local-session boundaries; do not claim Cowork/cloud compatibility.

## Project Structure

```text
integrations/agent-skills/
├── Install.ps1
└── brownpants-context/SKILL.md
integrations/README.md
tests/AgentSkillIntegrationTests.ps1
.github/workflows/ci.yml
specs/029-local-agent-context-skills/{spec,plan,research,data-model,quickstart,tasks,verification}.md
```

**Structure Decision**: Keep a single canonical skill beside a small host-selectable installer. Installed copies use `.agents/skills/brownpants-context/SKILL.md` for Codex and `.claude/skills/brownpants-context/SKILL.md` for Claude Code. Existing OpenSpec and Copilot adapters remain independent because they define separate workflows and invocation surfaces.

## Complexity Tracking

No constitution violations or new runtime dependencies.
