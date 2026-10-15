# Quickstart: Copilot GUI Context Workflow

## Prerequisites

- PowerShell 7 (`pwsh`) for installation.
- BrownPants installed or otherwise available as a local command when Copilot can run terminal commands.
- GitHub Copilot Chat configured in Visual Studio 2022 17.10+ or VS Code Local harness. Organization policies may disable terminal tools.

## Install

From the BrownPants checkout:

```powershell
pwsh -NoProfile -File ./integrations/copilot/Install.ps1 -ProjectRoot <target-repository>
```

Expected output names `.github/prompts/brownpants-context.prompt.md`.

## Use

Open the target project in Copilot Chat and invoke `/brownpants-context`. If the host cannot run terminal commands, run `brownpants inspect . --format markdown --limit 50` yourself and paste the report. Verify reported paths and relationships against source files.

## Verify installation behavior

```powershell
pwsh -NoProfile -File ./tests/CopilotPromptIntegrationTests.ps1
```

Expected output: `Copilot prompt integration tests passed.` The test validates prompt guidance and absent, identical, and conflicting installation states; it does not launch Visual Studio or VS Code.
