# Implementation Plan: Managed and Native Interop Analysis

**Branch**: `001-managed-native-interop` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/001-managed-native-interop/spec.md`

## Summary

Extend BrownPants' deterministic repository inventory to help developers follow legacy managed clients into MFC/native code and locate factory and dependency wiring. Reuse Roslyn syntax analysis for C# and VB.NET, retain explicit candidate labeling for lexical native C++ signals, and report project metadata and source locations in Markdown and JSON. Keep the default scan independent of target builds.

## Technical Context

**Language/Version**: C# on .NET 10; analyze legacy C#, VB.NET, and native C++ source.
**Primary Dependencies**: .NET 10 SDK; Roslyn 4.14; MSBuild 17.14 for opt-in semantic project loading.
**Storage**: None; scan repository files and emit reports.
**Testing**: xUnit fixture-based tests; `dotnet test BrownPants.sln`.
**Target Platform**: .NET 10 command-line host; source repositories may target .NET Framework and Windows native toolchains.
**Project Type**: CLI application.
**Performance Goals**: Complete ordinary static scans without building target projects; cap report samples by the existing `--limit` setting.
**Constraints**: Deterministic, offline static inspection by default. Native C++ findings are lexical candidates, not full semantic analysis. Opt-in MSBuild semantic analysis can evaluate imports and design-time targets.
**Scale/Scope**: Repository-local inventory and source evidence; no cross-repository index or runtime tracing.

## Constitution Check

The checked-in `.specify/memory/constitution.md` contains only the generated placeholder template and no ratified principles or gates. No constitution constraints can be evaluated. Use the concrete repository guidance in `AGENTS.md` and existing analyzer/report patterns: deterministic output, explicit evidence limits, fixture-based tests, and no source relationship claims beyond analyzed evidence.

## Project Structure

### Documentation (this feature)

```text
specs/001-managed-native-interop/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/inspect-report.md
└── tasks.md                 # produced by speckit-tasks
```

### Source Code (repository root)

```text
src/BrownPants.Cli/
├── Analysis/                # repository, language, project, and pattern analyzers
├── Output/                  # Markdown reporting; JSON serializes inventory records
└── Program.cs               # inspect command and options
tests/BrownPants.Tests/      # xUnit temporary-repository fixtures
integrations/speckit/        # BrownPants context command and extension builder
```

**Structure Decision**: Keep feature work in the existing single CLI project, analyzer/report layers, and xUnit project. Do not introduce another assembly or a database for repository-local reports.

## Complexity Tracking

No constitution gate violations identified. The repository has no ratified constitution to supply additional gates.
