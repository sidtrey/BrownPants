# BrownPants Copilot chat prompt

This adapter provides a repository-shareable, manually invoked context workflow for GitHub Copilot Chat. It uses BrownPants' existing local CLI; it does not install or call an AI-specific CLI or SDK, run a service, or access the network.

## Supported hosts

- **Visual Studio 2022 17.10 or later**: Copilot Chat supports prompt files under `.github/prompts/`. Invoke `/brownpants-context` or select the prompt from the chat prompt picker.
- **VS Code Local harness**: prompt files are discovered from `.github/prompts/` and can be invoked as `/brownpants-context`.
- **VS Code Copilot Agent mode**: this harness uses agent skills and does not load `.prompt.md` files. Install the shared skill with `integrations/agent-skills/Install.ps1 -Agent copilot`; see the [agent skill guide](../agent-skills/README.md).
- **Visual Studio 2026 18.5 or later**: Copilot agent mode discovers workspace agent skills; use the shared skill installer. Earlier Visual Studio versions can use this prompt adapter.

Prompt and skill discovery, selection, and terminal execution depend on host version, settings, and organization policy. Skill discovery does not guarantee that the agent activates it. The workflow never bypasses a denied capability. If the agent cannot run terminal commands, the developer can run BrownPants from an approved local shell and paste the report into chat.

## Install

From this repository, run:

```powershell
pwsh -NoProfile -File ./integrations/copilot/Install.ps1 -ProjectRoot <target-repository>
```

The installer copies the bundled prompt to `.github/prompts/brownpants-context.prompt.md`. It creates missing directories, leaves identical installations unchanged, and refuses to replace a different file or follow a destination link outside the project. Installation is local-only. If the destination conflicts, review it manually and rerun after resolving the conflict.

The prompt expects the `brownpants` command to be available when the Copilot host is allowed to execute terminal commands. BrownPants itself targets .NET 10. See the [root quick start](../../README.md#quick-start) for building and installing the tool. The prompt does not install software when the command is missing.

For VS Code Copilot Agent mode or Visual Studio 2026, install the reusable skill instead of this prompt file:

```powershell
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent copilot
```

## What it does

The prompt requests a bounded, read-only Markdown inventory, then asks Copilot to verify relevant paths against repository source, project, test, and SDD files. It distinguishes candidates from observed facts and reports evidence limits. It does not run semantic MSBuild loading, claim root cause, or modify source files during context collection.

Run the repository integration checks with:

```powershell
pwsh -NoProfile -File ./tests/CopilotPromptIntegrationTests.ps1
```

These checks cover prompt guidance and installer behavior. They do not launch Visual Studio or VS Code.
