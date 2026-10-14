# Research: Later-Phase Spec Kit Context Refresh

## Extension lifecycle events

Spec Kit 1.0+ supports lifecycle hooks before constitution, specification, clarification, planning, checklist, task generation, cross-artifact analysis, implementation, and task-to-issues conversion, plus after implementation. A manifest hook maps an event to a provided command, with `optional: false` denoting a mandatory hook. All ten events can share the BrownPants context command.

## Codex hook execution

Spec Kit's generated Codex workflow skills inspect `.specify/extensions.yml`, surface mandatory hooks, then invoke the command skill before continuing. The existing BrownPants context command is already installed as a Codex skill; the two new lifecycle events can reuse it without adding command files.

## Phase-aware evidence

- `specify`: use ecosystem and language coverage to avoid assuming a stack.
- `plan`: consider project markers, references, tests, target frameworks, and candidate architecture patterns.
- `tasks`: check current source paths, build/test commands, manifests, and active feature artifacts before naming implementation tasks.
- `implement`: re-read active plan/tasks and relevant current source/project files; use the inventory to navigate, not as a substitute for source review.

## Safety and limits

Default scans remain static and offline. Semantic Roslyn mode is not used automatically because MSBuild evaluation may execute imported targets. Candidate pattern signals are leads requiring source verification. Parser and project-load issues do not establish build failures.
