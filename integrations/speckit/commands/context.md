---
description: Build an evidence-based picture of the existing repository for the active Spec Kit step.
---

# BrownPants repository context

## User input

$ARGUMENTS

## Required steps

1. Run `brownpants inspect . --format markdown --limit 10` from the repository root. If the command is not on `PATH`, try the repo-local tool shim: `./.tools/brownpants.exe inspect . --format markdown --limit 10` on Windows or `./.tools/brownpants inspect . --format markdown --limit 10` on Unix-like systems. The limit keeps the initial context compact; the report gives total counts and omitted-sample notices. Do not infer project structure from Spec Kit artifacts alone.
2. If neither the command nor a repo-local shim is available, tell the user that BrownPants must be installed before repository evidence can be gathered. Do not substitute guesses for scanner output.
3. Treat BrownPants output as a navigation map. Read the relevant source, project, test, and Spec Kit files before making claims about behavior or architecture.
4. Keep observed facts separate from requirements and proposed changes. Cite paths and line numbers for source claims. Report parser errors, unresolved calls, or missing project references as investigation leads, not proof of defects.
5. Apply evidence to the active Spec Kit phase:
   - For `constitution`, distinguish current repository conventions from normative principles; do not infer requirements from existing code alone.
   - For `specify`, use ecosystem and language-coverage sections to understand the existing stack without assuming a single language or build system.
   - For `clarify`, inspect relevant current behavior and tests to identify what is known versus genuinely unspecified; leave product decisions to the user.
   - For `plan`, account for manifests across detected ecosystems, .NET/native targets, language versions, references, tests, and source-located factory, DI, COM, C++/CLI, or MFC signals.
   - For `checklist`, ground repository-facing checklist items in actual source, tests, and project configuration; do not treat a candidate signal as proof.
   - For `tasks`, compare the active spec and plan with current source paths, manifests, tests, and supported analysis so tasks name real files and usable project commands.
   - For `analyze`, compare active spec, plan, tasks, and checklists with current repository evidence; report mismatches as findings for review, not automatic requirements.
   - For `implement`, reread the active plan and tasks plus the relevant current source/project files. Check for repository drift and verify claims directly in source before editing.
   - After `implement`, compare the refreshed inventory and worktree diff with completed task claims. Review actual test/build output before reporting completion; an inventory change is not proof that behavior passes.
   - For `taskstoissues`, check that task IDs, descriptions, and completion states in the active tasks artifact match current evidence; do not create or modify external issues as part of this context step.
   C# and VB.NET findings are syntax-based; native C++ findings are lexical candidates; other listed languages are inventory-only unless the report says otherwise. Inspect both sides of managed/native boundaries. Do not turn candidate matches into confirmed relationships or new product requirements.
6. If the bounded report omits evidence needed for the task, rerun with a higher `--limit` or inspect the relevant files directly. If semantic call resolution is necessary, explain that `brownpants inspect . --semantic --limit 10` loads MSBuild projects and may evaluate imported build targets. Use it only when project files are trusted and the additional resolution is relevant.

## Completion

Summarize the evidence used, its limits, and the paths that should be inspected in the next Spec Kit phase. Do not modify application source code as part of this context command.
