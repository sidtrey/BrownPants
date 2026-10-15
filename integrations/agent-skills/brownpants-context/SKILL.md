---
name: brownpants-context
description: Gather and verify BrownPants evidence about an existing repository before planning, investigating a bug, or changing code. Use when a task needs brownfield context.
---

# BrownPants Repository Context

Use this workflow when the user asks for repository analysis, implementation planning, bug investigation, or change impact. Treat BrownPants output as a navigation map, then verify relevant findings in source, project, test, and SDD files.

## Gather evidence

1. From the repository root, request the bounded, read-only report:

   ```text
   brownpants inspect . --format markdown --limit 10
   ```

2. If `brownpants` is unavailable, check for the repository-local tool shim: `.tools/brownpants.exe` on Windows or `.tools/brownpants` on Unix-like systems. Do not install tools or use an AI host CLI or SDK.
3. Do not add `--semantic`. Default inspection avoids MSBuild project evaluation and provides syntax-only invocation evidence. Semantic project loading is opt-in and may execute imported build targets.
4. Read the relevant source, project, test, and SDD files before making source-level claims. Cite paths and line numbers when available.

## State evidence limits

- Separate observed facts from candidate or heuristic signals. A candidate pattern is a lead to verify, not a proven relationship.
- Roslyn analysis of C# and Visual Basic is syntax-based unless the report explicitly contains semantic results. Invocation names alone do not resolve target methods.
- C/C++ pattern matches are lexical candidates. Inspect both sides of managed/native boundaries before describing an MFC, COM, or C++/CLI relationship.
- Other languages are inventory-only unless the report explicitly states otherwise.
- Inventory, parse diagnostics, or unresolved symbols alone do not prove buildability, runtime behavior, active dependencies, test coverage, or root cause.
- Report missing project references, parse issues, and unresolved calls as investigation leads rather than defects.

## Respect host and organization policy

- If the host cannot run terminal commands, the `brownpants` command is missing, or policy disallows its use, do not retry through another AI CLI, change permissions, or bypass the restriction.
- Ask the user to run BrownPants in an approved environment and provide the report, or to provide an existing report. Continue only from evidence available in the conversation; do not invent findings.
- Repository paths and project structure may be sensitive. Remind the user to review a report before pasting it into a chat.
- Do not access the network, run project builds, evaluate project files, or modify repository files as part of context collection.
