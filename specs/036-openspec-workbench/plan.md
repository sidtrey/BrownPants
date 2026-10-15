# Implementation Plan: OpenSpec Review Workbench

**Branch**: `036-openspec-workbench` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Extend `workbench` with explicit `--engine openspec` support. Select a unique non-archived change or require explicit selection, safely read its OpenSpec artifacts, and reuse the static HTML and note-export experience. Keep default Spec Kit behavior intact.

## Technical Context

**Language/Version**: C# / .NET 10  
**Dependencies**: Existing .NET libraries only; no new package  
**Storage**: Selected OpenSpec change files and user-selected HTML output  
**Testing**: xUnit temporary repository fixtures and CLI entry point  
**Targets**: Windows, Linux, macOS; modern browser  
**Constraints**: Offline static report; strict bounded UTF-8; no reparse points; no source edits; all paths under repository root.

## Constitution Check

- Brownfield and engine coverage: pass; add one built-in adapter using the established OpenSpec folder convention.
- Safety and evidence: pass; read only the selected change, preserve raw content, and label checkbox state as recorded data.
- Determinism: pass; stable relative artifact paths and requirement ordering.
- Dependency and privacy constraints: pass; no runtime package, server, network, or external service.

## Design

Keep engine selection and path validation in `SddWorkbenchAnalyzer`; represent engine and dynamic section labels in `SddWorkbenchData`. For OpenSpec, enumerate immediate directories under `openspec/changes`, excluding `archive`; when no explicit selection exists, require exactly one. Read `proposal.md`, optional `design.md`/`tasks.md`, and sorted `specs/**/spec.md` files under per-file, total-byte, artifact-count, directory-count, and traversal-entry bounds. Parse requirement and scenario headings into stable path-qualified note IDs. Render all original files as escaped text, with an OpenSpec-specific overview and requirements heading. Preserve the existing Spec Kit implementation path and output defaults.

## Project Structure

- `src/BrownPants.Cli/Analysis/SddWorkbenchAnalyzer.cs`
- `src/BrownPants.Cli/Output/SddWorkbenchHtmlReport.cs`
- `src/BrownPants.Cli/Program.cs`
- `tests/BrownPants.Tests/SddWorkbenchTests.cs`

## Risks

OpenSpec schemas may evolve. This feature recognizes documented path and heading conventions, retains raw source, and does not claim full OpenSpec validation. Large or linked artifacts fail closed with an actionable error.
