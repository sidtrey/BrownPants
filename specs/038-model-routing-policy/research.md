# Research: Task-Based Model Guidance

**Reviewed**: 2026-10-15

## Host support findings

| Host | Native task profile | Model selection evidence | Product limit |
|---|---|---|---|
| VS Code / Copilot | Workspace `.github/agents/*.agent.md` custom agents | Official custom agent metadata supports a `model` field and handoff model; the planning guide documents selecting a stronger planning model and a separate implementation model. | Account/model availability and actual agent/handoff invocation remain controlled by VS Code and the user. The selected model is not a task scheduler. |
| Visual Studio 18.4+ | Repository `.github/agents/*.agent.md` custom agents | Official Visual Studio agent metadata supports an optional `model` field; custom agents require Visual Studio 18.4 or later. | Keep generated filenames distinct from VS Code profiles in the shared directory. Older versions and model availability are unverified/manual. |
| Claude Code | Project `.claude/agents/*.md` subagents | Official subagent frontmatter includes `model`; model values can use aliases or full identifiers. | A configured subagent is used only if invoked or delegated to. Model availability and fallback are runtime matters. |
| Codex | Project `.codex/agents/*.toml` custom agents | Official subagent documentation supports `model` and reasoning settings in custom agent files and a default model for spawned agents. | Host delegation and selected model access are controlled by Codex. A custom agent does not force every matching task to run through it. |

Sources checked on 2026-10-15:

- VS Code custom-agent locations and metadata: https://code.visualstudio.com/docs/agent-customization/custom-agents
- VS Code planning and choosing planning/implementation models: https://code.visualstudio.com/docs/agents/run/planning
- VS Code model availability and selection: https://code.visualstudio.com/docs/agent-customization/language-models
- Visual Studio custom agents, project directory, model property, and minimum version: https://learn.microsoft.com/visualstudio/ide/copilot-specialized-agents
- Claude Code custom subagents and model frontmatter: https://code.claude.com/docs/en/sub-agents
- Claude Code session model selection: https://code.claude.com/docs/en/cli-usage
- Codex custom agent model configuration: https://learn.chatgpt.com/docs/agent-configuration/subagents
- Codex configuration reference: https://learn.chatgpt.com/docs/config-file/config-reference

## Design decisions

- Keep one repository-owned policy and make all native exports explicit, reviewable, and project-local.
- Preserve the exact model names selected by the team. Validate syntax and presence; do not check account access or recommend a current model catalog.
- Use three standard quality tiers (`economy`, `balanced`, `high`) as labels only. They carry no price or quality guarantee.
- Support the task categories documented by the schema and reject unknown categories so a typo cannot silently route a task to the wrong profile.
- Export only one explicitly selected host at a time. Generate one host agent file per profile. Visual Studio and VS Code share `.github/agents/`, so use `brownpants-visual-studio-<id>.agent.md` for Visual Studio and `brownpants-<id>.agent.md` for VS Code. Never write user-level settings or infer that a profile will automatically be used.
- Emit a report identifying hosts with no native exporter as unsupported/manual, rather than mapping them to a similar host format.
- Use .NET built-in JSON support and standard library file APIs; add no NuGet package.

## Rejected approaches

- Automatically choose current model products or publish price recommendations: the user's account, enterprise policy, privacy rules, and model access vary; catalogs and prices change.
- Treat instructions inside `AGENTS.md` or task text as runtime enforcement: external hosts may ignore instructions or use their currently selected model.
- Call provider APIs to test availability: this would require credentials, create network/data-handling concerns, and violate offline deterministic operation.
