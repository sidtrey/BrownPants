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

## Request a caller diagram

When the user asks which code calls a particular C# or Visual Basic method, first identify its exact compiler symbol with `brownpants inspect <repository> --semantic --format markdown --limit 10000`. Project evaluation can execute imported MSBuild targets, so request the user's/policy's permission before choosing this opt-in analysis when it has not already been authorized. Then run:

```text
brownpants chart callers <repository> --source <relative.cs-or-vb-path> --method '<exact-fully-qualified-symbol>' --semantic --output <relative.png> --depth 8 --limit 1000
```

The chart follows compiler-resolved callers only. It marks test paths as candidates, shows partial coverage and sampled relationships, and saves a sibling `.drawio` source. Do not describe candidate test paths as executed coverage or infer dynamic, reflection, native, or unresolved calls as edges.

## Request a method behavior diagram

When the user asks what one method does, use `brownpants chart flow` with its exact C#/VB symbol and repository-relative source path:

```text
brownpants chart flow <repository> --source <relative.cs-or-vb-path> --method '<exact-fully-qualified-symbol>' --semantic --output <relative.png>
```

It maps Roslyn control-flow blocks, decisions, loops, returns, and explicit throw/rethrow paths. It does not model implicit runtime exceptions. Full block operations and source locations remain in the editable `.drawio` sidecar; complex results may be tiled.

## Choose a chart family

- **Process flow**: use a `brownpants-flow` block whose nodes, meanings, and directed connections are explicitly supplied.
- **Organization chart**: use a `brownpants-org` block with named people and declared reporting links. It must form one rooted tree; BrownPants rejects multiple managers and ambiguous structures.
- **Brainstorm map**: use a `brownpants-brainstorm` block with named ideas and declared parent-child branches. It must form one rooted tree.
- **Caller/impact tree**: use the exact C#/VB symbol and explicit semantic workflow above. Test paths are candidates only.
- **Method behavior**: use `chart flow` to show the selected C#/VB method's compiler control-flow graph; distinguish explicit compiler paths from unmodeled runtime behavior.

If BrownPants cannot parse a source language or cannot resolve a requested relationship, you may assemble a `brownpants-flow` input from evidence already supplied or inspected. Cite file/line evidence in node or edge details, label the result as agent-assembled, and do not imply that its relationships were compiler-resolved. Render it with `brownpants diagram`; BrownPants validates the chart structure and draws it locally without parsing that source language.

Do not invent relationships to fill gaps. For unsupported chart requests, explain the supported families and offer the closest useful source-backed chart, or ask the user for the missing relationships.

## Respect host and organization policy

- If the host cannot run terminal commands, the `brownpants` command is missing, or policy disallows its use, do not retry through another AI CLI, change permissions, or bypass the restriction.
- Ask the user to run BrownPants in an approved environment and provide the report, or to provide an existing report. Continue only from evidence available in the conversation; do not invent findings.
- Repository paths and project structure may be sensitive. Remind the user to review a report before pasting it into a chat.
- Do not access the network, run project builds, evaluate project files, or modify repository files as part of context collection.
