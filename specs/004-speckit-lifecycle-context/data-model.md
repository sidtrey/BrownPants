# Data Model: Later-Phase Spec Kit Context Refresh

No BrownPants report fields or application data types change.

## Extension hook registrations

Each lifecycle registration includes:

- **Event**: one of `before_constitution`, `before_specify`, `before_clarify`, `before_plan`, `before_checklist`, `before_tasks`, `before_analyze`, `before_implement`, `before_taskstoissues`, `after_implement`.
- **Command**: `speckit.brownpants.context` for all events.
- **Optional**: `false`, requiring the workflow to run the context command before continuing.
- **Description**: short phase-specific reason to refresh the repository inventory.

## Context input and output

The input remains the target repository and active Spec Kit phase. The command emits the same Markdown/JSON inventory as before; Codex applies the report to that phase and verifies claims in the cited source files.
