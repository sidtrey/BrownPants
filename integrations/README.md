# Workflow integrations

BrownPants keeps repository scanning and the JSON/Markdown evidence contract independent of SDD engines. Adapters consume those reports and add workflow-specific instructions without changing scanner behavior.

## Supported adapters

- [`speckit/`](speckit/README.md): Spec Kit extension with mandatory context refreshes before constitution, specification, clarification, planning, checklist, task, analysis, implementation, and issue-sync phases, and after implementation. It also offers an optional bug-assessment evidence hook when Spec Kit's bundled bug extension is installed.
- [`openspec/`](openspec/README.md): Explicit Codex skill for OpenSpec proposal and implementation workflows. Installs separately under `.agents/skills/` and preserves OpenSpec-managed skills.

Adapters must state their invocation model, tool prerequisites, supported workflows, and evidence limits. Do not describe a manual skill as an automatic hook or claim analysis that the core report does not provide.
