# Workflow integrations

BrownPants keeps repository scanning and the JSON/Markdown evidence contract independent of SDD engines. Adapters consume those reports and add workflow-specific instructions without changing scanner behavior.

## Supported adapters

- [`speckit/`](speckit/README.md): Spec Kit extension with mandatory context refreshes before constitution, specification, clarification, planning, checklist, task, analysis, implementation, and issue-sync phases, and after implementation. It also offers an optional bug-assessment evidence hook when Spec Kit's bundled bug extension is installed.
- [`openspec/`](openspec/README.md): Explicit Codex skill for OpenSpec proposal and implementation workflows. Installs separately under `.agents/skills/` and preserves OpenSpec-managed skills.
- [`copilot/`](copilot/README.md): User-invoked GitHub Copilot chat prompt for Visual Studio 2022 and VS Code Local harness, plus a route to the agent skill for current Copilot harnesses.
- [`agent-skills/`](agent-skills/README.md): Shared project-local BrownPants context skill for Codex, Claude Code, and supported Copilot GUI hosts, with a safe offline installer and policy-respecting user-run report fallback.
- [`azure-devops/`](azure-devops/README.md): Opt-in Azure DevOps Services traceability with system-browser Entra sign-in, plus explicit headless token support and a separate draft PR handoff.

Adapters must state their invocation model, tool prerequisites, supported workflows, and evidence limits. Do not describe a manual skill as an automatic hook or claim analysis that the core report does not provide.

## Local diagrams

The core `brownpants diagram` command renders the project's own `brownpants-flow` format to PNG or native `.drawio` XML. It is not an SDD-engine adapter and does not parse Mermaid. Rendering stays in the process, reads and writes only repository-relative paths, and does not contact a service or invoke another renderer. See the [flowchart guide](../specs/032-local-text-flowcharts/quickstart.md) for syntax, limits, and commands.
