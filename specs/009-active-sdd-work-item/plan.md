# Implementation Plan: Report the Active SDD Work Item

**Branch**: `009-active-sdd-work-item` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/009-active-sdd-work-item/spec.md`

## Summary

Add explicit active work item selection to the engine-neutral SDD inventory. Validate the Spec Kit feature pointer against safe repository-relative scanned paths, select an OpenSpec change only when its non-archived candidate is unique, and expose the path plus evidence label in Markdown and JSON.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: .NET base class library and System.Text.Json already used by the analyzer
**Storage**: Read-only repository files; no persistent state
**Testing**: xUnit temporary repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: Parse at most one small Spec Kit pointer file and preserve existing linear inventory behavior
**Constraints**: No external tools, MSBuild evaluation, network, writes, out-of-repository reads, or path-following; deterministic diagnostics and report ordering
**Scale/Scope**: Built-in Spec Kit and OpenSpec selection; custom workflows remain inventory-only

## Constitution Check

- **Evidence First**: Every selection has an explicit evidence label; no selection is made from bounded sample ordering. PASS.
- **Broad, Engine-Neutral Core**: A generic nullable active work item fits the core model; only built-in adapters supply supported selection evidence. PASS.
- **Deterministic and Reviewable Output**: Paths, labels, diagnostics, and JSON additions are stable. PASS.
- **Safe Inspection by Default**: Read one bounded in-repository pointer, reject reparse points and unsafe paths, verify targets against scanned files. PASS.
- **Specified and Verified Changes**: Fixture tests cover valid, ambiguous, malformed, unsafe, stale, Markdown, and JSON cases. PASS.

## Design

`SddWorkflowInventory` gains nullable `ActiveWorkItemPath` and `ActiveWorkItemEvidence` fields at the end of the record. `SddWorkflowAnalyzer` reads `.specify/feature.json` only if it occurs in the scanned file list, validates and normalizes `feature_directory`, and confirms that at least one scanned file is beneath the target. Malformed metadata generates a stable `SddWorkflowConfigurationIssue`; unsafe raw values are not echoed. OpenSpec selection uses all discovered first-level change directories and selects only a single non-archived candidate. Custom workflow records retain null selection fields. Markdown and the default JSON serializer expose the new values consistently.

### Expected Repository Changes

```text
src/BrownPants.Cli/Analysis/RepositoryInventory.cs
src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs
src/BrownPants.Cli/Output/MarkdownReport.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
README.md
```

## Validation

Run `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, and both Markdown and JSON CLI inspections. Confirm the installed Spec Kit and OpenSpec adapter checks remain unaffected.

## Complexity Tracking

No constitution violations or new dependencies.
