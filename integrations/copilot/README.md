# BrownPants Copilot chat prompt

This adapter provides a repository-shareable, manually invoked context workflow for GitHub Copilot Chat. It uses BrownPants' existing local CLI; it does not install or call an AI-specific CLI or SDK, run a service, or access the network.

## Supported hosts

- **Visual Studio 2022 17.10 or later**: Copilot Chat supports prompt files under `.github/prompts/`. Invoke `/brownpants-context` or select the prompt from the chat prompt picker.
- **VS Code Local harness**: prompt files are discovered from `.github/prompts/` and can be invoked as `/brownpants-context`.
- **VS Code Copilot harness**: `.prompt.md` files are not loaded by this harness. Use a supported customization for that harness; this adapter does not claim compatibility with it.

Prompt discovery and terminal execution depend on host settings and organization policy. The workflow never bypasses a denied capability. If the agent cannot run terminal commands, the developer can run BrownPants from an approved local shell and paste the report into chat.

## Install

From this repository, run:

```powershell
pwsh -NoProfile -File ./integrations/copilot/Install.ps1 -ProjectRoot <target-repository>
```

The installer copies the bundled prompt to `.github/prompts/brownpants-context.prompt.md`. It creates missing directories, leaves identical installations unchanged, and refuses to replace a different file or follow a destination link outside the project. Installation is local-only. If the destination conflicts, review it manually and rerun after resolving the conflict.

The prompt expects the `brownpants` command to be available when the Copilot host is allowed to execute terminal commands. BrownPants itself targets .NET 10. See the [root quick start](../../README.md#quick-start) for building and installing the tool. The prompt does not install software when the command is missing.

## What it does

The prompt requests a bounded, read-only Markdown inventory, then asks Copilot to verify relevant paths against repository source, project, test, and SDD files. It distinguishes candidates from observed facts and reports evidence limits. It does not run semantic MSBuild loading, claim root cause, or modify source files during context collection.

Run the repository integration checks with:

```powershell
pwsh -NoProfile -File ./tests/CopilotPromptIntegrationTests.ps1
```

These checks cover prompt guidance and installer behavior. They do not launch Visual Studio or VS Code.
