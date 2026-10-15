# BrownPants local agent skills

This adapter installs one evidence-first context skill into a project for Codex or Claude Code. It works with local host sessions and uses BrownPants' existing local CLI; it does not invoke an AI-specific CLI or SDK, call a service, or access the network.

## Install

From the BrownPants checkout, install the Codex skill, Claude Code skill, or both into an existing target repository:

```powershell
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent codex
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent claude
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent both
```

Installation copies the bundled `SKILL.md` into `.agents/skills/brownpants-context/` for Codex, `.claude/skills/brownpants-context/` for Claude Code, or both. Existing identical files are left unchanged. Different files are preserved and reported as conflicts. The both-host option checks both destinations before writing. The installer requires an existing project root, contains writes to that root, and performs no downloads.

## Use

- **Codex CLI and IDE extension**: invoke `$brownpants-context` or select it from the host's skill list.
- **Codex in the ChatGPT desktop app**: select the project skill in the app's Skills interface for the target repository.
- **Claude Code Desktop local session**: invoke `/brownpants-context` or select the project skill from the slash-command list.

The workflow requests a bounded, read-only Markdown inventory and then verifies relevant claims against source, project, test, and SDD files. Default invocation evidence is syntax-only; native C/C++ matches are lexical candidates; other languages are inventory-only unless the report says otherwise. It does not claim runtime behavior, buildability, or root cause from inventory alone.

If the host cannot run commands, BrownPants is unavailable, or policy disallows terminal access, the skill asks the user to run BrownPants in an approved environment or provide an existing report. It does not bypass settings or invent findings. Review repository paths and project structure before sharing a report in chat.

Project-skill discovery depends on host version, workspace trust, and organization policy. Claude Code Cowork and cloud sessions have different skill loading rules; this adapter only claims support for local Claude Code sessions. See the vendor guides for [Codex skills](https://developers.openai.com/codex/skills), [Claude Code skills](https://code.claude.com/docs/en/skills), and [Claude Code Desktop](https://code.claude.com/docs/en/desktop).

Run repository checks with:

```powershell
pwsh -NoProfile -File ./tests/AgentSkillIntegrationTests.ps1
```

The checks validate skill content and installer behavior. They do not launch Codex or Claude Code GUI sessions.
