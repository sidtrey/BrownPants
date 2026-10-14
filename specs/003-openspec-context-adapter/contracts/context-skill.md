# Context Skill Contract

## Invocation

- Codex skill name: `openspec-brownpants-context`.
- User invokes it explicitly before OpenSpec `propose` or `apply` workflows.
- No lifecycle hook or automatic OpenSpec invocation is registered.

## Required behavior

1. Run `brownpants inspect . --format markdown`; if unavailable, try `./.tools/brownpants.exe` on Windows or `./.tools/brownpants` on Unix-like systems.
2. If neither executable is available, stop and report the prerequisite.
3. Summarize detected ecosystems, source-language analysis levels, relevant project/test paths, and report limits.
4. Identify relevant OpenSpec source specs in `openspec/specs/` and active change artifacts in `openspec/changes/`. If more than one change may apply and the request does not identify one, ask which change is in scope.
5. Separate observed repository facts from OpenSpec requirements and proposed decisions. Cite paths and line numbers for source claims.

## Prohibited claims

The skill must not describe inventory-only languages as parsed, candidate matches as verified relationships, or manifest presence as proof that a project builds. It must not modify files during a context-only invocation.

## Installation contract

`Install.ps1` installs only the bundled skill under `.agents/skills/openspec-brownpants-context/SKILL.md`. An identical existing file is a successful no-op. A different existing file is preserved and causes a clear nonzero failure. No OpenSpec-generated skill files are modified.
