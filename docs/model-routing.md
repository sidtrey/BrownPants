# Task-Based Model Guidance

BrownPants lets a team describe a model preference for categories such as planning, code implementation, testing, review, or routine documentation. The team chooses the quality tier and exact model identifiers. BrownPants does not rank models, check account access, contact providers, estimate price, or promise that a host will route a task to a matching profile.

## Set up a policy

Copy the sample into the repository-local policy path, then replace its example model identifiers with names available in your host:

```powershell
New-Item -ItemType Directory -Force .brownpants | Out-Null
Copy-Item examples/model-routing-policy.json .brownpants/models.json
```

Edit `.brownpants/models.json`. Each profile has a kebab-case ID, one or more categories, a `qualityTier` (`economy`, `balanced`, or `high`), a short rationale, one or more host-to-model mappings, and task instructions. Categories are `analysis`, `complex-debugging`, `specification`, `planning`, `task-breakdown`, `implementation`, `testing`, `review`, `documentation`, and `routine-maintenance`. The tier is a team label, not a cost or quality guarantee. Model identifiers remain exactly as entered.

```powershell
dotnet run --project src/BrownPants.Cli -- models validate .
dotnet run --project src/BrownPants.Cli -- models report . --format markdown
dotnet run --project src/BrownPants.Cli -- models report . --format json
```

`report` can also show the host formats BrownPants can generate. A missing policy is valid and remains read-only; validation/reporting say that no assignments are configured.

## Export a host profile

Choose one host explicitly. BrownPants preflights every destination, preserves conflicting files, and treats identical exports as current.

```powershell
dotnet run --project src/BrownPants.Cli -- models export . --host vscode
dotnet run --project src/BrownPants.Cli -- models export . --host visual-studio
dotnet run --project src/BrownPants.Cli -- models export . --host claude
dotnet run --project src/BrownPants.Cli -- models export . --host codex
```

Generated files go under `.github/agents/`, `.claude/agents/`, or `.codex/agents/`. Visual Studio 18.4+ and VS Code/Copilot share `.github/agents/`; VS Code files use `brownpants-<id>.agent.md`, and Visual Studio files use `brownpants-visual-studio-<id>.agent.md` with a host-specific display name. Visual Studio's `model` frontmatter value is optional and works only for models available in that host version/account. Older Visual Studio versions are manual/unverified. Review generated instructions before using them. A host or user still has to select or invoke the profile; BrownPants cannot make an external agent delegate a matching task to it. Unknown host names remain visible in reports as manual, unverified preferences but do not have an exporter.

## Current host evidence

Documentation checked on **2026-10-15** confirms these project-local model fields:

- [VS Code custom agents](https://code.visualstudio.com/docs/agent-customization/custom-agents) use workspace `.github/agents/*.agent.md` files and support a `model` field. [VS Code planning guidance](https://code.visualstudio.com/docs/agents/run/planning) describes choosing a planning model and then a separate implementation model. Model picker access depends on the harness, organization settings, and account.
- [Visual Studio custom agents](https://learn.microsoft.com/visualstudio/ide/copilot-specialized-agents) use repository `.github/agents/*.agent.md` files, support the optional `model` field, and require Visual Studio 18.4 or later. The file describes configuration support; actual models vary by Visual Studio version and Copilot access.
- [Claude Code custom subagents](https://code.claude.com/docs/en/sub-agents) use project `.claude/agents/*.md` files and support `model` in YAML frontmatter. The subagent runs only when selected or delegated to.
- [Codex custom agents](https://learn.chatgpt.com/docs/agent-configuration/subagents) use project `.codex/agents/*.toml` files and support a `model` field. Codex controls delegation and the selected model must be available to the account/workspace.

These sources document configuration formats, not BrownPants runtime activation. Host formats, model names, pricing, and access can change. No new provider dependency or network call is made by this feature.
