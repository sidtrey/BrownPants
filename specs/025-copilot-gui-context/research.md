# Research: Copilot GUI Context Workflow

## Decisions

- Use a manually invoked `.prompt.md` in `.github/prompts/`. Visual Studio Copilot supports this workspace location and chat invocation. VS Code uses it in Local harness sessions.
- Do not automatically add or edit `.github/copilot-instructions.md`; a one-shot context workflow avoids injecting it into every chat or colliding with team guidance.
- Provide a PowerShell installer that copies a checked-in asset, creates only the needed prompt directory, treats identical content as a no-op, and refuses a different destination file.
- Ask for `brownpants inspect . --format markdown --limit 50`. If terminal access or the command is unavailable, ask the user to run BrownPants or paste a report; do not invent findings, install tools, or access the network.
- Document that VS Code's Copilot harness does not load `.prompt.md` files; current VS Code docs support them only in Local harness sessions. Do not claim universal host support.

## Rationale

Official Visual Studio documentation locates reusable prompt files at `.github/prompts/`, supports `#prompt:` and `/` invocation, and lists Visual Studio 2022 17.10+ as a prerequisite. Official VS Code documentation uses the same workspace folder and manual invocation but scopes prompt files to Local harness sessions. This shared asset provides a GUI route without an AI-specific CLI or SDK. Organization policy still determines whether the host exposes terminal access.

## Alternatives Considered

- Automatic repository custom instructions: rejected because they run across unrelated chats, add permanent prompt context, and can conflict with existing team instructions.
- A VS Code extension or MCP server: deferred because it adds runtime distribution, host permissions, and a separate tool process; enterprise policy may disable these capabilities.
- One cross-host agent skill: deferred because Visual Studio documents prompt files, while VS Code distinguishes prompts by harness; one file cannot honestly claim identical discovery across hosts.

## Sources

- VS Code prompt files: https://code.visualstudio.com/docs/agent-customization/prompt-files
- Visual Studio Copilot Chat context and prompt files: https://learn.microsoft.com/en-us/visualstudio/ide/copilot-chat-context?view=vs-2022
- VS Code customization migration and harness behavior: https://code.visualstudio.com/docs/agent-customization/migrate-customizations
