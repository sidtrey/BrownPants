# Quickstart: Later-Phase Spec Kit Context Refresh

1. Build and install the BrownPants .NET tool in a Spec Kit project.
2. Install or update the BrownPants Spec Kit extension from `integrations/speckit/`.
3. Confirm `specify artifact list --json` reports ten active mandatory BrownPants hooks: `before_constitution`, `before_specify`, `before_clarify`, `before_plan`, `before_checklist`, `before_tasks`, `before_analyze`, `before_implement`, `before_taskstoissues`, and `after_implement`.
4. Run `$speckit-tasks` or `$speckit-implement` in Codex. Each phase should emit and execute the mandatory `speckit.brownpants.context` command before continuing.
5. Verify candidate findings and inventory-only language rows against the actual source before treating them as implementation facts.

The automatic hook uses the globally installed BrownPants command or the repo-local `.tools` shim. Semantic analysis remains a separate opt-in scan.
