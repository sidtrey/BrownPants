# Contract: Copilot GUI Context Integration

## Prompt

- Installed file: `.github/prompts/brownpants-context.prompt.md`.
- Invoke from supported Copilot chat with `/brownpants-context` or the prompt picker.
- Request `brownpants inspect . --format markdown --limit 50` when terminal access and the command are available.
- Treat output as a navigation map; inspect cited source, project, and test files before making claims.
- Never use `--semantic`, install tools, access network resources, claim root cause, or edit source files as part of inventory.
- If execution is unavailable or fails, ask the user to run the command or paste its output. Report the limitation and do not fabricate findings.
- Produce a concise summary of repository evidence, verification performed, and evidence limits.

## Installer

`Install.ps1 [-ProjectRoot <existing-directory>]`

- Copies the bundled prompt to `.github/prompts/brownpants-context.prompt.md`.
- Creates missing `.github/prompts/` directories inside the project root only.
- If the destination is byte-identical, exits successfully without rewriting it.
- If a different file or directory occupies the destination, exits unsuccessfully and preserves it.
- Does not access the network, execute the target project, or edit source files.
