# BrownPants agent skills for GUI hosts

This adapter installs one evidence-first context skill into a project for Codex, Claude Code, and supported GitHub Copilot agent hosts. It uses BrownPants' existing local tool; it does not invoke an AI-specific CLI or SDK, call a service, or access the network.

## Install

From the BrownPants checkout, install the Codex skill, Claude Code skill, or both into an existing target repository:

```powershell
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent codex
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent claude
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent both
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent copilot
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent all
```

Installation copies the bundled `SKILL.md` to `.agents/skills/brownpants-context/` for Codex, `.claude/skills/brownpants-context/` for Claude Code, and `.github/skills/brownpants-context/` for Copilot. `both` retains its original Codex-plus-Claude meaning; `all` selects all three hosts. Existing identical files are left unchanged. Different files are preserved and reported as conflicts. Multi-host options check every destination before writing any. The installer requires an existing project root, contains writes to that root, and performs no downloads.

## Use

- **Codex CLI and IDE extension**: invoke `$brownpants-context` or select it from the host's skill list.
- **Codex in the ChatGPT desktop app**: select the project skill in the app's Skills interface for the target repository.
- **Claude Code Desktop local session**: invoke `/brownpants-context` or select the project skill from the slash-command list.
- **GitHub Copilot in VS Code Agent mode**: `.github/skills/` is a supported workspace location. The skill may be selected based on its description; discovery does not guarantee activation. Choose it from the host's skill UI or mention BrownPants context when needed.
- **GitHub Copilot in Visual Studio**: workspace skills are supported in Visual Studio 2026 version 18.5 or later. Earlier Visual Studio versions can use the prompt workflow described below.
- **GitHub Copilot app and cloud agent**: current VS Code documentation lists committed project skills for these hosts. Check host and organization availability before relying on the workflow.

Copilot's current agent harness uses skills for reusable workflows and does not load `.prompt.md` files. Visual Studio 2022 and VS Code Local harness users can use the separate [Copilot prompt adapter](../copilot/README.md).

The workflow requests a bounded, read-only Markdown inventory and then verifies relevant claims against source, project, test, and SDD files. Default invocation evidence is syntax-only; native C/C++ matches are lexical candidates; other languages are inventory-only unless the report says otherwise. It does not claim runtime behavior, buildability, or root cause from inventory alone.

If the host cannot run commands, BrownPants is unavailable, or policy disallows terminal access, the skill asks the user to run BrownPants in an approved environment or provide an existing report. It does not bypass settings or invent findings. Review repository paths and project structure before sharing a report in chat.

Project-skill discovery depends on host version, workspace trust, and organization policy. Availability does not guarantee that the model selects a skill. The skill never bypasses terminal restrictions: if BrownPants cannot run, it asks the user to provide a report generated in an approved environment. Claude Code Cowork and cloud sessions have different skill loading rules; this adapter claims support only for local Claude Code sessions. See the vendor guides for [VS Code agent skills](https://code.visualstudio.com/docs/agent-customization/agent-skills), [Visual Studio agent skills](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-agent-skills?view=visualstudio), [Codex skills](https://developers.openai.com/codex/skills), [Claude Code skills](https://code.claude.com/docs/en/skills), and [Claude Code Desktop](https://code.claude.com/docs/en/desktop).

Run repository checks with:

```powershell
pwsh -NoProfile -File ./tests/AgentSkillIntegrationTests.ps1
```

The checks validate skill content and installer behavior. They do not launch Codex or Claude Code GUI sessions.
