# Acceptance Criteria Review

## Proposed change

- Previous acceptance criteria digest: `0e0ae3f52216f83cd2712d4453f593e1e7695f22d83c5c28165f8377b4582ed7`
- Proposed acceptance criteria digest: `9db28707e8d0f1125d5f02dee9f52c02bfbd723b7fe9a921970c04942ea432dc`
- Changed IDs: `AC-004`, `FR-005`, `SC-003`; User Story 2 description also updated.

## Reason

The repository's standing product direction is to support Visual Studio and VS Code users equally. Official Visual Studio documentation describes project-local custom agents with model selection starting in Visual Studio 18.4. The original criteria covered VS Code/Copilot, Claude Code, and Codex but omitted Visual Studio.

Source: [Use GitHub Copilot agents in Visual Studio](https://learn.microsoft.com/visualstudio/ide/copilot-specialized-agents). It specifies repository agents in `.github/agents/`, the optional `model` front matter property, and the Visual Studio 18.4+ requirement for custom agents.

## Proposed behavior

Add Visual Studio 18.4+ as a supported export target. Its generated agent profile must use a distinct filename from VS Code profiles because both use `.github/agents/`. Older Visual Studio versions remain unverified/manual.

## Maintainer decision

**Accepted 2026-10-15.** The maintainer explicitly approved the Visual Studio 18.4+ expansion and requested the acceptance baseline, implementation plan, and tasks be updated. The baseline was refreshed to the proposed digest `9db28707e8d0f1125d5f02dee9f52c02bfbd723b7fe9a921970c04942ea432dc` before implementation. Planned scope includes host serializers, CLI choices, documentation, example policy, tests, and packed-tool smoke coverage.
