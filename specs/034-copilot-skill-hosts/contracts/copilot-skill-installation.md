# Copilot Skill Installation Contract

`integrations/agent-skills/Install.ps1` accepts these host selections:

| `-Agent` | Destination |
| --- | --- |
| `codex` | `.agents/skills/brownpants-context/SKILL.md` |
| `claude` | `.claude/skills/brownpants-context/SKILL.md` |
| `copilot` | `.github/skills/brownpants-context/SKILL.md` |
| `both` | Codex and Claude destinations (backward compatible) |
| `all` | All three destinations |

For every selected destination, the installer must resolve containment beneath the existing project root. Identical files are unchanged. A different file, directory, link, or escaping destination fails before any selected destination is written. The installed bytes match the bundled skill exactly.
