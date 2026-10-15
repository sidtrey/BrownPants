# Research: Cursor BrownPants Context Command

## Cursor project commands

Cursor documents custom commands as Markdown files under `.cursor/commands/`; the filename becomes the slash command shown in chat. The feature is marked beta in the vendor documentation, so BrownPants must describe this as a Cursor-specific integration and make no cross-version activation guarantee. Source: [Cursor custom commands](https://docs.cursor.com/en/agent/chat/commands).

## Installation behavior

Follow the existing Copilot prompt installer contract: require an existing project directory, copy one bundled file, leave identical content untouched, preserve conflicts, and refuse symlink/reparse-point escapes. The Cursor integration test should inspect the command contract and exercise installer behavior in a temporary project; it cannot prove Cursor UI activation.

## Terminal-restricted environments

The command can ask Cursor to run BrownPants only when the host grants terminal access. If not, it must request a report from an approved shell or accept a user-supplied report. This keeps organization controls authoritative and gives a GUI user a reviewable path forward.
