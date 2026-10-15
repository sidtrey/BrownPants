---
name: brownpants-context
description: Inspect a repository with BrownPants and verify relevant evidence before planning or changing code.
---

# BrownPants Repository Context

Use this prompt when a repository task needs a reliable map of the existing codebase. This workflow is read-only; do not change application files.

## Collect evidence

1. If the current Copilot host allows terminal commands and `brownpants` is available, run `brownpants inspect . --format markdown --limit 50` from the repository root.
2. If terminal access is unavailable or restricted by organization policy, do not try to bypass that restriction. Ask the user to run that command locally and paste its output.
3. If the command is unavailable or fails, show the exact diagnostic and ask the user to install or locate BrownPants themselves, or provide an existing report. Do not download or install anything, and do not fabricate scanner findings.
4. Use the inventory as a navigation map. Open the relevant source, project, test, and SDD files before making claims about behavior or relationships. Cite paths and line numbers when available.

## Report carefully

- Separate directly observed repository facts from candidate patterns and inferences.
- State relevant evidence limits. File or project markers do not prove that code builds, runs, or is active. Syntax and lexical candidates are not proof of runtime relationships or root cause.
- Do not claim that a candidate is resolved, covered by tests, or responsible for a bug without direct evidence.
- Do not use semantic/MSBuild inspection for this context pass; it may evaluate imported build targets.
- Do not access the network or modify repository files during this inventory step.

Return a concise summary with repository areas to inspect, supporting paths, checks actually performed, and remaining evidence gaps. If the user asked for implementation, use the evidence to plan the work and follow the repository's contributor and acceptance-criteria instructions.

## Host support

This repository prompt is intended for Visual Studio Copilot Chat and VS Code's Local harness. In VS Code, `.prompt.md` files are not loaded by the Copilot harness; use a supported host customization there. Prompt discovery and terminal permissions depend on host settings and organization policy. The prompt itself does not grant those permissions.
