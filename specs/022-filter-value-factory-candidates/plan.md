# Implementation Plan: Filter Value-Returning Factory Candidates

**Branch**: `022-filter-value-factory-candidates` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: `specs/022-filter-value-factory-candidates/spec.md`

## Summary

Exclude string-returning C# and VB.NET helpers from factory-method candidates based solely on a `Create`, `Build`, or `Resolve` prefix. Preserve object-returning candidates and verify against BrownPants itself.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing Roslyn C# and Visual Basic syntax APIs
**Storage**: None
**Testing**: xUnit temporary-repository fixtures
**Target Platform**: Windows, Linux, macOS
**Project Type**: .NET CLI
**Performance Goals**: No material scan-time change
**Constraints**: Deterministic syntax-only analysis; no project builds or semantic model
**Scale/Scope**: C# and VB.NET factory-method candidate classification

## Constitution Check

- Evidence First: PASS. Findings remain candidates with source locations.
- Broad, Engine-Neutral Core: PASS. This is core source analysis, not Spec Kit-specific.
- Deterministic and Reviewable Output: PASS. No output schema or ordering change.
- Safe Inspection by Default: PASS. The scanner remains read-only and offline.
- Specified and Verified Changes: PASS. Criteria are frozen and implementation tasks will cite observed evidence.

## Project Structure

### Feature Documentation

`specs/022-filter-value-factory-candidates/` contains the spec, research, data model, contract, quickstart, checklist, and tasks.

### Source and Tests

- `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`
- `tests/BrownPants.Tests/RepositoryScannerTests.cs`

**Structure Decision**: Extend the existing analyzer and temporary-repository tests. No project or report-schema changes.

## Complexity Tracking

No constitution violations.