# BrownPants repository context

When the user invokes `/brownpants-context`, gather local BrownPants evidence before discussing or changing code.

If Cursor allows terminal execution and the `brownpants` command is available, run this read-only command from the target repository root:

```text
brownpants inspect . --format markdown --limit 50
```

Use its output as a navigation map and verify relevant claims against the source, project, test, and SDD files. Distinguish observed facts from candidate or heuristic signals, cite repository paths and line numbers, and state what the report cannot establish. Do not run semantic/MSBuild mode, builds, tests, or network requests as part of context collection. Do not modify repository files while collecting context.

If terminal access is denied, `brownpants` is unavailable, or host policy prevents execution, do not retry through another tool or attempt to bypass the restriction. Ask the user to run BrownPants from an approved local shell and paste the report, or provide an existing report. Until a report is available, do not invent repository findings.

This command does not install software or guarantee that Cursor will discover or invoke it. Host command support, terminal access, and activation depend on the installed Cursor version and organization settings.
