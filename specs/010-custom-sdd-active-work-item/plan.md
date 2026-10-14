# Implementation Plan: Configure Active Work Items for Custom SDD Engines

**Branch**: `010-custom-sdd-active-work-item` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/010-custom-sdd-active-work-item/spec.md`

## Summary

Add optional `activeWorkItemPath` selection to custom workflow declarations. Validate it against safe relative path rules, declared artifact roots, and the existing scanned file list; expose its path and evidence through the existing engine-neutral report fields.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: .NET base class library and System.Text.Json already used by the custom workflow parser
**Storage**: Read-only `.brownpants/sdd-workflows.json`; no selection state is written
**Testing**: xUnit temporary repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: Preserve linear inventory processing with bounded existing configuration parsing
**Constraints**: No new file reads, external commands, network access, MSBuild evaluation, or report schema version change
**Scale/Scope**: One optional field per custom workflow; built-in workflow selection remains unchanged

## Constitution Check

- **Evidence First**: A configured path and its evidence label are explicit; no guessing from samples. PASS.
- **Broad, Engine-Neutral Core**: Any workflow can declare the same safe path without engine-specific code. PASS.
- **Deterministic and Reviewable Output**: Normalized paths and stable diagnostics are shared across Markdown and JSON. PASS.
- **Safe Inspection by Default**: Validate against roots and scanned files; do not read the target. PASS.
- **Specified and Verified Changes**: Tests cover valid, absent, unsafe, outside-root, and stale selection. PASS.

## Design

Recognize `activeWorkItemPath` as an allowed optional workflow property. Normalize separators and validate it with the same portable repository-relative segment rules used for Spec Kit selection. Preserve a valid workflow definition when only its selection is invalid. During custom workflow analysis, require the path to be a strict descendant of a declared artifact root and to prefix at least one path in the existing file list. Emit a stable declaration diagnostic for failures. Pass valid path and `CustomWorkflowDeclaration` into `SddWorkflowInventory`; workflows with no valid explicit selection retain null fields.

### Expected Repository Changes

```text
src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
README.md
specs/005-pluggable-sdd-discovery/contracts/custom-workflow-config.md
```

## Validation

Run `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, Spec Kit and OpenSpec adapter checks, and inspect Markdown/JSON for valid and invalid custom paths.

## Complexity Tracking

No constitution violations or new dependencies.
