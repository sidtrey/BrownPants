---
name: openspec-brownpants-context
description: Inspect BrownPants repository evidence before proposing or implementing an OpenSpec change.
---

# OpenSpec BrownPants Context

Use this skill explicitly before `$openspec-propose` or `$openspec-apply-change`. OpenSpec does not call this skill automatically.

## Gather repository evidence

1. Run `brownpants inspect . --format markdown --limit 10` from the repository root.
2. If `brownpants` is unavailable, try `./.tools/brownpants.exe inspect . --format markdown --limit 10` on Windows or `./.tools/brownpants inspect . --format markdown --limit 10` on Unix-like systems. The limit keeps initial context compact; total counts and omitted-sample notices show when more evidence is available.
3. If neither command is available, report that BrownPants must be installed and stop. Do not guess about the codebase.
4. Treat the report as a navigation map. Read the relevant source, project, tests, and OpenSpec artifacts before making behavior or architecture claims.

## Apply evidence to OpenSpec

- For a proposal, review current behavior specs under `openspec/specs/` and any related active changes under `openspec/changes/`.
- For implementation, identify the requested change and inspect its existing `proposal.md`, `design.md`, `tasks.md`, and delta specs under `specs/`. These files may be absent or customized; use the repository's actual layout.
- If multiple active changes could match and the request does not identify one, ask which change is in scope.
- Keep observed repository facts, OpenSpec requirements, and proposed design decisions distinct. Cite repository paths and line numbers for source claims.
- Describe each language only at the analysis level BrownPants reports. Inventory-only files are not parsed; candidate patterns are leads, not verified relationships; manifest presence does not prove a project builds.
- If the bounded report omits evidence needed for the task, rerun with a higher `--limit` or inspect relevant files directly. Use `brownpants inspect . --semantic --limit 10` only when relevant and project files are trusted; it may evaluate MSBuild imports and targets.

## Completion

Summarize the report evidence, its limits, and the OpenSpec artifacts/source paths to inspect next. This context-only skill must not modify repository files.
