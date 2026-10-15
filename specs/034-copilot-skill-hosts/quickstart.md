# Quickstart: BrownPants in Copilot GUIs

Install the reusable agent skill for Copilot in VS Code or supported Visual Studio versions:

```powershell
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot . -Agent copilot
```

To install for Codex, Claude Code, and Copilot in one operation:

```powershell
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot . -Agent all
```

The skill is installed at `.github/skills/brownpants-context/SKILL.md`. In Copilot agent hosts, skills can be selected by the host based on the task; availability does not guarantee activation. If needed, choose the skill from the host's skill UI or mention BrownPants context explicitly.

Visual Studio 2022 and VS Code Local harness users can continue using the `.github/prompts/` workflow documented at [the Copilot prompt guide](../../integrations/copilot/README.md). If terminal access or BrownPants is unavailable, run the tool in an approved environment and provide its report; do not bypass policy.
