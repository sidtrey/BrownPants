# Implementation Plan: JavaScript and TypeScript Focused Evidence

**Branch**: 077-js-ts-focused-evidence | **Date**: 2026-11-06 | **Spec**: spec.md

## Summary

Extend `evidence --changed` validation to the JavaScript/TypeScript extensions already indexed by BrownPants. Keep managed and native change paths on `CallImpactAnalyzer`; use bounded JS/TS syntax relationships to select uniquely linked caller candidates and label them distinctly.

## Technical Context

- **Language**: .NET 10 / C# 12
- **CLI**: `src/BrownPants.Cli/EvidenceCommand.cs` and `Program.cs`
- **Source evidence**: `JavaScriptTypeScriptAnalysis.CallRelationships` and `EvidenceReportAnalyzer`
- **Focused selection**: `EvidenceFocusAnalyzer`
- **Existing impact**: `CallImpactAnalyzer` supports C#/VB semantic calls and C/C++ native-flow candidates only.
- **Tests**: xUnit fixture-backed evidence tests in `EvidenceCommandTests.cs`.
- **Validation**: focused evidence tests, complete BrownPants build/test, and a real FullMetalFile React path handoff.

## Design

Accept the eight supported JS/TS extensions only after the existing repository-relative and discovered-file checks. Add source-located JS/TS call-candidate findings to the evidence report. For changed JS/TS targets, select only `syntax-resolved` relationships whose target path matches the changed source; use a distinct match reason and leave ambiguous/unresolved relationships unlinked. Split changed paths by analysis family before invoking `CallImpactAnalyzer`, preserving current C#/VB and C/C++ behavior. Report JS/TS analyzer truncation and syntax-only limitations in focus completeness and report limitations. Update command help and invalid-path guidance.

## Validation

- Focused `EvidenceCommandTests` for TS/TSX callers and mixed paths.
- `dotnet build BrownPants.sln` and `dotnet test BrownPants.sln`.
- `brownpants evidence <FullMetalFile-root> --changed frontend/src/App.tsx --format json`.
- Acceptance criteria hash and evidence ledger validation.
