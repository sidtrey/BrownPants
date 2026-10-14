---
name: speckit-brownpants-context
description: Build an evidence-based picture of the existing repository for the active
  Spec Kit step.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: BrownPants contributors
  source: extension:brownpants
---

# Brownpants Context Skill

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
   - For `implement`, verify the active feature's `acceptance-criteria.sha256` with `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath <active-spec-path>` before editing. If no baseline exists, initialize and record one before implementation. If verification fails, stop implementation and report the changed acceptance text; refresh only after explicit user review. Then reread the active plan/tasks and relevant source/project files.
   - After `implement`, compare the refreshed inventory and worktree diff with completed task claims. Review actual test/build output before reporting completion; an inventory change is not proof that behavior passes.
   - For `taskstoissues`, check that task IDs, descriptions, and completion states in the active tasks artifact match current evidence; do not create or modify external issues as part of this context step.
   C# and VB.NET findings are syntax-based; native C++ findings are lexical candidates; other listed languages are inventory-only unless the report says otherwise. Inspect both sides of managed/native boundaries. Do not turn candidate matches into confirmed relationships or new product requirements.
6. If the bounded report omits evidence needed for the task, rerun with a higher `--limit` or inspect the relevant files directly. If semantic call resolution is necessary, explain that `brownpants inspect . --semantic --limit 10` loads MSBuild projects and may evaluate imported build targets. Use it only when project files are trusted and the additional resolution is relevant.
7. When the user asks to map instruction context, run `brownpants instructions . --format markdown` (add `--source <relative-path>` for custom instruction files). Treat scope as heuristic, token counts as approximate, and section anchors as navigation aids. Do not flatten or rewrite canonical instruction files.
8. When the user asks for a compact response frame, ask the active agent to draft a JSON file with `budget` (`unit`: `words` or `tokens`, `requested`), ordered `points` (`text` and cited `sources` with `path` and optional `anchor`), and optional `reviewItems`. Validate and render it with `brownpants frame create . ./frame-draft.json docs/agent-context-frame.md`, then check freshness with `brownpants frame validate . docs/agent-context-frame.md`. BrownPants checks source and section references, records source digests, reports budget overflow, and protects existing output unless `--overwrite` is explicit. Review semantic summaries and conflicts manually. A frame only reduces loaded context if the host is configured to use it instead of duplicating its source files.
9. At implementation completion, verify the same acceptance-criteria digest again. Report a mismatch instead of redefining “done”; run the build and tests, then map each original acceptance scenario to observed evidence.
10. When a contributor asks where a proposed rule belongs, use `brownpants placement` with the candidate text and the contributor's explicit `scope`, `durability`, and `purpose`. Pass untrusted candidate text as a distinct process argument, not interpolated shell syntax. If an attribute is unknown, use `unknown` and present BrownPants' clarification questions. Show the recommendation and rationale; do not edit the constitution/spec/instruction files unless separately requested.

## Completion

Summarize the evidence used, its limits, and the paths that should be inspected in the next Spec Kit phase. Do not modify application source code as part of this context command.
