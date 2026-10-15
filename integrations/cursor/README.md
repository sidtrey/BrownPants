# BrownPants Cursor command

This adapter adds a repository-local `/brownpants-context` command to Cursor Chat. Cursor documents custom commands as Markdown files in `.cursor/commands/`; this feature is beta and host behavior may change. See [Cursor custom commands](https://docs.cursor.com/en/agent/chat/commands).

## Install

From a BrownPants checkout, install into an existing project:

```powershell
pwsh -NoProfile -File ./integrations/cursor/Install.ps1 -ProjectRoot <target-repository>
```

The installer copies `commands/brownpants-context.md` to `.cursor/commands/brownpants-context.md`. It is safe to rerun when the installed file matches. Different files are preserved as conflicts. The installer does not download or install BrownPants, invoke an AI CLI/SDK, or access the network.

## Use

In Cursor Chat, invoke `/brownpants-context`. If Cursor permits terminal access and `brownpants` is installed, the command requests a bounded, read-only repository inventory and asks the agent to verify claims against source, project, test, and SDD files. It does not invoke semantic MSBuild analysis or modify the repository during context collection.

When terminal access is disabled or the command is unavailable, the workflow asks for a report generated in an approved local shell or supplied by the user. It never tries a different execution path to bypass a restriction and does not invent repository findings.

Host discovery, command activation, and terminal access depend on Cursor version and organization settings. The integration tests validate the bundled command and installer behavior; they do not launch Cursor or prove signed-in GUI activation.

Run the installer regression check from the BrownPants repository:

```powershell
pwsh -NoProfile -File ./tests/CursorCommandIntegrationTests.ps1
```
