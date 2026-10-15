# Research: Copilot GUI Skill Hosts

## Findings

- VS Code documents project skill paths `.github/skills/`, `.claude/skills/`, and `.agents/skills/`; it notes that skill availability does not guarantee use by the agent.
- VS Code Copilot harness sessions use agent skills for reusable workflows and do not load `*.prompt.md` files. Prompt files remain available to Local harness sessions.
- Visual Studio 2026 18.5+ documents the same workspace skill locations and automatic discovery in agent mode.
- Visual Studio prompt files remain documented under `.github/prompts/` for earlier versions and workflows.

## Decision

Add `.github/skills/brownpants-context/SKILL.md` to the existing installer. Keep `.agents/skills` and `.claude/skills` targets. Preserve the distinct prompt adapter and explain which workflow applies to each host/harness.

## Primary Sources (checked 2026-10-14)

- [Use Agent Skills in VS Code](https://code.visualstudio.com/docs/agent-customization/agent-skills) — skill locations, syntax, auto-selection limits, and Copilot host support.
- [Use Agent Skills with GitHub Copilot in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-agent-skills?view=visualstudio) — Visual Studio 2026 18.5+ prerequisites, discovery, and workspace locations.
- [Customize chat responses in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-chat-context?view=visualstudio) — prompt-file location and invocation.
- [Migrate customizations to the Copilot harness in VS Code](https://code.visualstudio.com/docs/agent-customization/migrate-customizations) — distinction between prompt files and Copilot harness skills.
