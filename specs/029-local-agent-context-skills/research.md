# Research: Local Agent Context Skills

**Date**: 2026-10-14

## Findings

- Codex documents project skills at `.agents/skills/`, with `$skill-name` invocation in its CLI/IDE workflows. Its current docs also describe standalone skills in the ChatGPT desktop app and Codex hosts. Source: [Codex skills](https://developers.openai.com/codex/skills).
- Claude Code documents project skills at `.claude/skills/<name>/SKILL.md` and explicit `/skill-name` invocation. Claude Code Desktop local sessions expose project skills. Project customization and user/org policy can restrict discovery. Sources: [Claude Code skills](https://code.claude.com/docs/en/skills), [Claude Code Desktop](https://code.claude.com/docs/en/desktop).
- Claude Cowork and cloud sessions have different skill sources and do not generally load arbitrary local-machine skills; this feature limits its claim to local Claude Code sessions. Source: [Claude Code skills](https://code.claude.com/docs/en/skills#choose-where-skills-load).

## Decisions

1. Maintain one canonical, host-neutral `SKILL.md`; copy the same payload to the selected project's Codex and/or Claude Code skill directory.
2. Use explicit, local installation (`codex`, `claude`, or `both`). Do not add a service, MCP server, provider API integration, or auto-running host hook.
3. State host-specific invocation syntax in the integration guide, not in the shared skill body.
4. The workflow asks for BrownPants' default bounded report and source verification. It never requests semantic MSBuild loading, executes an AI host CLI, fetches content, modifies application files, or bypasses disabled tool permissions.
5. Test host file layouts and the installer; document that automated checks do not prove live GUI behavior.
