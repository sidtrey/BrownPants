# Implementation Plan: Filter Test Methods from Factory Signals

**Branch**: `031-filter-test-factory-signals` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Update Roslyn-based C# and VB architecture analysis to suppress factory-method candidates for recognized test-attribute methods and to locate factory evidence at the method declaration. Keep syntax-only findings labeled as candidates and preserve the existing return-type and name heuristics.

## Technical Context

**Language/Version**: C# on .NET 10; analyzer consumes C# and VB syntax trees.
**Primary Dependencies**: Roslyn C# and Visual Basic APIs already referenced by the CLI.
**Storage**: None.
**Testing**: xUnit; temporary repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
**Target Platform**: .NET 10 supported platforms.
**Project Type**: CLI with library-style analysis components.
**Performance Goals**: Linear syntax traversal; no additional project loading.
**Constraints**: Deterministic, offline, syntax-only default analysis; support C# and VB attribute syntax without semantic compilation.

## Constitution Check

- **Evidence First**: report candidates only; test attributes are syntactic markers, not semantic type resolution.
- **Broad, Engine-Neutral Core**: change applies to repository analysis and does not depend on Spec Kit or another SDD engine.
- **Deterministic and Reviewable Output**: stable source traversal and existing report formats are retained; evidence points to declaration lines.
- **Safe Inspection by Default**: no MSBuild evaluation, network, or source execution is introduced.
- **Specified and Verified Changes**: acceptance criteria are hashed; fixture tests cover both languages and output formats.

All gates pass. No constitution exception is required.

## Project Structure

### Feature artifacts

```text
specs/031-filter-test-factory-signals/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/architecture-pattern-evidence.md
```

### Source and tests

```text
src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs
src/BrownPants.Cli/Analysis/RepositoryInventory.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
```

**Structure Decision**: Keep the behavior in the existing architecture-pattern analyzer and extend its scanner fixture tests; no new project or public CLI option is needed.

## Design

Recognize the explicitly scoped xUnit, NUnit, and MSTest attribute names from each method's C# or VB attribute lists. Match the final syntactic name segment case-insensitively, optionally removing the `Attribute` suffix; do not require a framework package or compilation. Suppress only the method's factory-method candidate. For emitted C#/VB method findings, use the method statement/declaration location and normalized signature text, avoiding leading attributes and trivia. Preserve existing factory name/return checks and unrelated signals.

## Verification

Add fixtures for all listed markers, qualified and suffixed names, arguments, multiple attributes, and production factories with leading attributes/comments. Assert candidate category, evidence, line, and report behavior for JSON and Markdown. Run `dotnet test BrownPants.sln -c Release`, verify the acceptance hash and evidence ledger, and confirm self-inspection no longer reports the two named submission tests.

## Complexity Tracking

No constitution violations or additional architectural components.
