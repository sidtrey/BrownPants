# Implementation Plan: Cross-Ecosystem Repository Inventory

**Branch**: `002-cross-ecosystem-inventory` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/002-cross-ecosystem-inventory/spec.md`

## Summary

BrownPants must provide useful, language-neutral inventory for every repository while making specialized source-analysis coverage explicit and keeping the evidence model independent of any SDD engine. Add ecosystem manifest grouping, common-language coverage rows, and extensionless-file counts. Preserve deeper Roslyn and MFC/interop analyzers as clearly bounded specializations; Spec Kit remains an adapter over the same report.

## Technical Context

**Language/Version**: C# on .NET 10
**Primary Dependencies**: Existing .NET and Roslyn dependencies; no new packages
**Storage**: None; inventory is computed per scan
**Testing**: xUnit temporary-repository fixtures; `dotnet test BrownPants.sln`
**Target Platform**: .NET 10 CLI on supported .NET 10 platforms; inspect arbitrary local repositories
**Project Type**: CLI application
**Performance Goals**: Linear in discovered files, with deterministic grouping and no project builds
**Constraints**: Offline and read-only scan; manifest-name classification only; never imply parsing for inventory-only languages; preserve Markdown/JSON parity
**Scale/Scope**: Common application, native, package, and build systems; unknown extensions remain visible even when no ecosystem rule matches

## Constitution Check

The project constitution is an unfilled generated template, so no ratified principles or gates apply. Repository guidance requires deterministic output, source evidence limits, fixture tests, and avoiding unsupported relationship claims. The plan preserves these constraints.

## Research Summary

- Keep `ProjectMarkers` as the existing path-level evidence and add grouped `ProjectSystems` rather than replacing a public JSON field.
- Classify ecosystems from conventional manifest basenames and path suffixes; do not parse manifest contents or execute package/build commands.
- Add `LanguageCoverage` rows from source extensions, reporting existing C#/VB syntax analysis, native C/C++ lexical candidates, and inventory-only status for other recognized languages.
- Represent extensionless files explicitly in `Extensions` so file totals reconcile.
- Render the same stored records in Markdown and JSON; use stable path and label ordering.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── RepositoryScanner.cs              # file inventory and manifest integration
├── RepositoryInventory.cs            # additive inventory records
├── ProjectSystemAnalyzer.cs          # manifest-to-ecosystem grouping
└── SourceLanguageCoverageAnalyzer.cs # extension-to-analysis coverage
src/BrownPants.Cli/Output/MarkdownReport.cs
src/BrownPants.Cli/BrownPants.Cli.csproj # package README path
README.md
specs/002-cross-ecosystem-inventory/
tests/BrownPants.Tests/RepositoryScannerTests.cs
```

**Structure Decision**: Extend the existing CLI project and report model. Use focused analyzers for project-system and language-coverage classification; do not add packages, assemblies, or target-project execution.

## Design Gates

- Ecosystem names and their filename rules are explicit and testable.
- Language coverage labels describe actual analyzer behavior, not inferred toolchain capability.
- Every added report field is additive and documented in the report contract.
- Tests compare Markdown and JSON evidence and verify stable output.

## Complexity Tracking

No constitution gate violations or additional project dependencies are introduced.
