# Implementation Plan: CLI Command Help

**Branch**: `028-cli-command-help` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/028-cli-command-help/spec.md`

## Summary

Add deterministic global, command-specific, and nested frame help before argument parsing so users can discover BrownPants commands and safely understand semantic analysis.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: Existing CLI and xUnit test project  
**Storage**: None  
**Testing**: `Program.Run` unit tests with captured output and error writers  
**Target Platform**: Windows, Linux, macOS  
**Project Type**: .NET CLI  
**Constraints**: No new dependencies; help must not inspect repositories or evaluate projects.

## Constitution Check

- Evidence First: help describes syntax and semantic evidence without overclaiming.
- Broad, Engine-Neutral Core: command help remains independent of SDD adapters.
- Deterministic and Reviewable Output: static text has stable ordering.
- Safe Inspection by Default: help routing precedes project loading and file writes.
- Specified and Verified Changes: acceptance scenarios map to captured-output tests.

## Project Structure

```text
src/BrownPants.Cli/Program.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
specs/028-cli-command-help/{spec,plan,research,tasks,verification}.md
```

**Structure Decision**: Keep help rendering beside existing CLI dispatch in `Program.cs`; exercise it through the existing test project.

## Complexity Tracking

No constitution violations or new architectural components.
