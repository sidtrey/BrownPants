# Installer Contract

## Command

```powershell
pwsh -NoProfile -File ./integrations/agent-skills/Install.ps1 -ProjectRoot <existing-project> -Agent <codex|claude|both>
```

## Behavior

- `codex` installs `brownpants-context/SKILL.md` at `.agents/skills/brownpants-context/SKILL.md`.
- `claude` installs the same source at `.claude/skills/brownpants-context/SKILL.md`.
- `both` preflights both destinations before writing and installs to both only if neither conflicts.
- An identical existing file is left untouched; a differing file or directory at the destination causes a nonzero error and remains unchanged.
- The selected root must exist and be a directory. Destination directories and files must remain within it; links/reparse points that redirect writes outside it are refused.
- The installer makes no network calls and does not invoke either AI host.
