# Implementation Plan: Local SDD Review Workbench

**Branch**: `035-sdd-workbench` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Add a `workbench` CLI command that creates a single self-contained HTML review page for one Spec Kit feature. It reads local feature artifacts, presents human-readable summaries and expandable source text, and lets a reviewer export notes as JSON. It does not edit the feature or run a server.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing .NET runtime and `System.Text.Json`; no new package
**Storage**: Input feature files and user-selected HTML output
**Testing**: xUnit fixture-based tests in `tests/BrownPants.Tests`
**Target Platform**: Windows, Linux, macOS; modern browser to view generated HTML
**Project Type**: CLI report generation
**Performance Goals**: Generate one workbench from a feature directory with a small bounded set of Markdown/JSON files
**Constraints**: No server, network calls, external assets, AI dependency, or writes to source artifacts; safe text encoding; output must remain under repository root
**Scale/Scope**: One Spec Kit feature per generated report

## Constitution Check

- Engine-neutral core direction: partial by design; v1 explicitly targets the current Spec Kit artifact contract and reports its scope.
- Safe inspection: pass; reads only a selected in-repository feature directory and does not evaluate MSBuild or execute project code.
- Deterministic and reviewable output: pass; stable ordering, no current timestamp, relative paths, no external assets.
- Specified and verified changes: pass; fixture-based tests cover parsing, paths, XSS-safe rendering, output protection, and note export.
- No new package or service: pass.

## Project Structure

```text
src/BrownPants.Cli/Analysis/SddWorkbenchAnalyzer.cs
src/BrownPants.Cli/Output/SddWorkbenchHtmlReport.cs
src/BrownPants.Cli/Program.cs
tests/BrownPants.Tests/SddWorkbenchTests.cs
```

**Structure Decision**: Keep selection and artifact parsing in Analysis, static HTML in Output, and command argument/output handling in Program. Avoid an ASP.NET host or browser extension so the report opens directly from disk.

## Design

Resolve `--feature` as a repository-relative directory; when omitted, read the unique active directory from `.specify/feature.json`. Reject malformed/outside paths and require `spec.md`. Read optional `tasks.md`, `acceptance-evidence.json`, `verification.md`, and `plan.md` only from the selected directory. Use relative paths and bounded file sizes.

Parse story headings, AC/SC identifiers, task checkboxes, and evidence ledger entries for concise summary cards. Preserve source Markdown in expandable text panels so unfamiliar content is not silently dropped. Render all source and path text with HTML encoding. Include only fixed inline CSS and JavaScript. The note-export UI creates a JSON Blob locally; note text is serialized with `System.Text.Json` and never inserted as markup. Protect existing output unless `--overwrite` is present.

## Complexity Tracking

No constitution violations. A focused Markdown recognizer is needed for clear story/task summaries; expandable original artifacts remain the authoritative content and preserve unsupported syntax.
