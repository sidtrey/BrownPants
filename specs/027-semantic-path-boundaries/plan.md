# Implementation Plan: Semantic Source Path Boundaries

**Branch**: `027-semantic-path-boundaries` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/027-semantic-path-boundaries/spec.md`

## Summary

Keep Roslyn semantic findings scoped to repository source trees so local package-cache files and external project paths cannot appear as semantic locations. Continue binding against external source and metadata so repository calls retain useful targets.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: Roslyn and MSBuildWorkspace already used by the CLI  
**Storage**: None  
**Testing**: xUnit with temporary source trees and a BrownPants self-scan  
**Target Platform**: Windows, Linux, macOS  
**Project Type**: .NET CLI  
**Constraints**: Semantic loading remains opt-in; paths must be stable and repository-relative; do not change JSON schema.

## Constitution Check

- Evidence First: findings retain source locations only for source inside the scanned root.
- Deterministic and Reviewable Output: root-relative paths use `/`; external locations are absent rather than machine-specific.
- Safe Inspection by Default: this change does not broaden default project evaluation; semantic analysis remains explicitly requested.
- Specified and Verified Changes: regression fixtures cover external trees and external target declarations; self-analysis verifies the original leak is gone.

## Project Structure

```text
src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
specs/027-semantic-path-boundaries/{spec,plan,research,tasks,verification}.md
```

**Structure Decision**: Keep containment logic in the existing semantic analyzer and extend its Roslyn fixture tests. No new project or dependency is needed.

## Complexity Tracking

No constitution violations or new architectural components.
