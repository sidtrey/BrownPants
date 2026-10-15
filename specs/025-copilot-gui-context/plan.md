# Implementation Plan: Copilot GUI Context Workflow

**Branch**: 025-copilot-gui-context | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: [Feature specification](spec.md)

## Summary

Ship a manually invoked GitHub Copilot prompt and safe local installer. Reuse BrownPants' existing read-only CLI for repository evidence; do not add an AI SDK, service, or host-specific runtime. The prompt must be usable from Visual Studio's .github/prompts support and VS Code's Local harness, and must explain the other VS Code harness limitation.

## Technical Context

**Language/Version**: Markdown and PowerShell 7-compatible scripts  
**Primary Dependencies**: Existing BrownPants CLI; PowerShell only for installation/checks  
**Storage**: Repository files only  
**Testing**: pwsh integration script and existing .NET build/test suite  
**Target Platform**: Windows, Linux, macOS for installer; Visual Studio 2022 and VS Code Copilot GUI for use  
**Project Type**: Cross-platform repository integration for a .NET CLI  
**Performance Goals**: Installer completes locally without network or project evaluation  
**Constraints**: No overwrite of conflicting user files; terminal access and prompt discovery are host/policy-controlled  
**Scale/Scope**: One prompt artifact, one installer, one integration test, and documentation

## Constitution Check

- Evidence First: prompt labels inventory as navigation and requires source verification.
- Broad, Engine-Neutral Core: no scanner or report contract changes; integration stays under integrations/copilot/.
- Deterministic and Reviewable Output: prompt requests Markdown with a bounded finding sample.
- Safe Inspection by Default: default inspect only; explicitly forbid semantic/MSBuild mode and network installation.
- Specified and Verified Changes: criteria baseline frozen; installer behavior covered by temporary-directory tests.

Gate: PASS. No constitution exception is needed.

## Phase 0: Research

See [research.md](research.md). Official docs confirm repository prompt files are available to Copilot in Visual Studio and in VS Code Local harness sessions. Newer VS Code Copilot harness sessions do not load .prompt.md files, so the integration guide must state that limitation clearly.

## Phase 1: Design & Contracts

- [data-model.md](data-model.md): static prompt artifact and install outcomes; no persistent runtime entities.
- [contracts/copilot-integration.md](contracts/copilot-integration.md): prompt and installer behavior.
- [quickstart.md](quickstart.md): local installation and host invocation checks.

## Project Structure

    integrations/copilot/
    ├── Install.ps1
    ├── README.md
    └── prompts/brownpants-context.prompt.md
    tests/CopilotPromptIntegrationTests.ps1
    .github/workflows/ci.yml
    integrations/README.md
    README.md

Structure Decision: Keep host-specific assets in integrations/copilot/, following the existing Spec Kit and OpenSpec adapter structure. Installer copies the prompt into .github/prompts/ in the user's repository; existing prompt files remain user-owned.

## Complexity Tracking

No constitution violations or extra runtime dependencies.