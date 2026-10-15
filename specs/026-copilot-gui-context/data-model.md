# Data Model: Copilot GUI Context Workflow

This feature has no persisted runtime data or network state.

## Prompt Artifact

- Source: `integrations/copilot/prompts/brownpants-context.prompt.md`
- Installed path: `<project>/.github/prompts/brownpants-context.prompt.md`
- Contents: invocation metadata, bounded command request, evidence review steps, and unavailable-tool fallback.
- Ownership: source is BrownPants-maintained; destination becomes user-owned. The installer does not replace divergent content.

## Installation Outcome

- `Installed`: destination was absent and the source was copied.
- `AlreadyCurrent`: destination matches source; no write occurs.
- `Conflict`: destination exists with different content; it is preserved and the script reports an error.
- `InvalidRoot`: target path does not identify an existing project directory.
