# Research: Verify Spec Kit Setup with Codex

## Decisions

- Use Spec Kit's non-interactive `init` command with Codex skills integration before extension installation.
- Validate representative generated skills (`speckit-specify`, `speckit-plan`, `speckit-implement`) and retain existing assertions for all ten BrownPants hooks.
- Keep the live check in the existing adapter test, which CI runs after installing a pinned Spec Kit CLI.

## Rationale

The previous live check installed the extension into a hand-created `.specify` directory. That verified extension packaging and hooks but did not prove the documented Codex setup creates the skills that invoke those hooks. A full temporary initialization closes that coverage gap.

## Alternatives Considered

- Test only the extension install: rejected because it omits Codex integration output.
- Require Codex CLI: rejected because Spec Kit emits Codex skills without launching the agent.
- Add a separate workflow job: rejected because the existing adapter job already provisions the required Spec Kit CLI.
