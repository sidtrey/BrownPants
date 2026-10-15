# Quickstart: Local Agent Context Skills

From the BrownPants checkout, install a host skill into an existing target repository:

```powershell
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <target-repository> -Agent both
```

Start or refresh the host's local project session. In Codex, invoke `$brownpants-context`; in Claude Code, invoke `/brownpants-context`. If repository skills or terminal access are disabled by policy, follow the skill's user-run report fallback and do not bypass the restriction.

Run adapter tests from this repository:

```powershell
pwsh -NoProfile -File ./tests/AgentSkillIntegrationTests.ps1
```

The test validates files, content, and installer safety. It does not launch or authenticate to either desktop host.
