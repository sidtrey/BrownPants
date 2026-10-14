# Implementation Plan: Later-Phase Spec Kit Context Refresh

**Branch**: `004-speckit-lifecycle-context` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Extend the BrownPants Spec Kit extension with mandatory context registrations across all nine supported lifecycle events. Update the shared context command, Codex skill copy, and integration documentation so repository evidence is applied appropriately to each phase. Keep the analyzer and report contract unchanged.

## Technical Context

**Language/Version**: YAML 1.0 extension manifest, Markdown instructions, .NET 10 CLI
**Primary Dependencies**: Spec Kit extension API, Codex Spec Kit skills, existing BrownPants tool
**Storage**: Repository files only; no new runtime state
**Testing**: Official `specify extension add` and hook introspection, Codex skill smoke tests for tasks and implement, xUnit suite
**Target Platform**: Codex CLI on Windows, macOS, and Linux
**Project Type**: SDD integration for a repository analysis CLI
**Performance Goals**: Existing default deterministic scan; no extra process beyond each configured hook
**Constraints**: All nine pre-events and the post-implementation event call the existing context command; `--semantic` stays opt-in; no scanner schema changes; require Spec Kit 1.0 or later.
**Scale/Scope**: Update extension manifests, shared context instructions, generated skill, and documentation

## Constitution Check

- Evidence honesty and deterministic scanner semantics: pass; no scanner changes.
- Brownfield refresh before implementation tasks and source edits: pass; hooks are mandatory.
- Avoid duplicated guidance: pass; one context command and synchronized Codex skill.
- Verification: pass; validate lifecycle registration and real Codex hook execution.

## Project Structure

```text
integrations/speckit/
├── extension.yml
├── extension.yml.template
├── commands/context.md
└── README.md
.specify/extensions/brownpants/
├── extension.yml
├── extension.yml.template
├── commands/context.md
└── README.md
.agents/skills/speckit-brownpants-context/SKILL.md
README.md
```

**Structure Decision**: Keep extension and generated local Spec Kit/Codex sources in sync. Update only hook metadata and phase instructions; preserve the installed command name and report format.

## Complexity Tracking

No new dependencies or application code.
