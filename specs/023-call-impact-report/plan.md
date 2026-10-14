# Implementation Plan: Call-Trace Change Impact Report

**Branch**: `023-call-impact-report` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: `specs/023-call-impact-report/spec.md`

## Summary

Add an opt-in `impact` command that accepts changed C#/VB.NET source paths and reports direct and transitive callers from MSBuild-loaded Roslyn call relationships. Identify likely test paths and provide graph-size/depth metrics to guide review, while marking partial evidence and avoiding effort estimates.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Roslyn, Microsoft.Build.Locator, MSBuildWorkspace
**Storage**: None
**Testing**: xUnit temporary .NET project/solution fixtures
**Target Platform**: Windows, Linux, macOS with loadable MSBuild projects
**Project Type**: .NET CLI
**Performance Goals**: Bounded output and traversal; use existing `--limit` ceiling
**Constraints**: Project loading may evaluate imports/design-time targets; no repository writes, test execution, or network access by BrownPants itself
**Scale/Scope**: C# and Visual Basic source methods in projects loadable by MSBuildWorkspace; native C++ and dynamic/runtime edges remain unresolved

## Constitution Check

- Evidence First: PASS. Each caller is a resolved symbol edge with a call-site location; tests remain candidates.
- Broad, Engine-Neutral Core: PASS. The report is CLI evidence usable by any SDD workflow.
- Deterministic and Reviewable Output: PASS. Stable symbol/path/depth ordering, bounded samples, explicit completeness.
- Safe Inspection by Default: PASS. `impact` is explicit opt-in and documents MSBuild evaluation; it does not run tests or edit files.
- Specified and Verified Changes: PASS. Frozen criteria map to fixtures and self-inspection evidence.

## Design Decisions

- Store target declaration path and line on resolved call edges so changed paths can map to loaded methods without fuzzy name matching.
- Traverse incoming edges breadth-first, retain each caller at its shortest discovered depth, and use a visited set to stop cycles.
- Mark the report incomplete when projects fail to load, calls are unresolved, call samples are truncated, or an input cannot be mapped to loaded declarations.
- Classify test candidates using existing path heuristics; never equate a matching test path with regression coverage.
- Report observed graph counts and depth as complexity evidence; do not convert them into time or story-point estimates.

## Project Structure

### Feature Documentation

`specs/023-call-impact-report/` contains the spec, plan, research, data model, contract, quickstart, quality checklist, tasks, evidence ledger, and verification record.

### Source and Tests

- `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`
- `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`
- New `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`
- New `src/BrownPants.Cli/Output/CallImpactReportRenderer.cs`
- `src/BrownPants.Cli/Program.cs`
- `tests/BrownPants.Tests/RepositoryScannerTests.cs`

**Structure Decision**: Add a focused report analyzer and renderer, reusing current semantic analysis and repository test-path inventory. Keep the inspect schema additive and create a separately versioned impact report.

## Complexity Tracking

No constitution violations.