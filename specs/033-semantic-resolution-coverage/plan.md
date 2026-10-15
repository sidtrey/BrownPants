# Implementation Plan: Semantic Resolution Coverage

**Branch**: `033-semantic-resolution-coverage` | **Date**: 2026-10-16 | **Spec**: [spec.md](spec.md)

## Summary

Make semantic call reports easier to trust and navigate by reconciling invocation counts, showing in-repository target locations, grouping unresolved binding reasons, and reporting complete project-load and compiler-error coverage. Load discovered projects that are not listed in solutions and deduplicate project paths across solutions.

## Technical Context

**Language/Version**: C# / .NET 10.  
**Primary Dependencies**: Existing Roslyn and MSBuildWorkspace packages; no new package.  
**Storage**: None.  
**Testing**: xUnit v3 and Microsoft.Testing.Platform fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.  
**Target Platform**: .NET 10 supported platforms, including Windows and Linux.  
**Project Type**: CLI.  
**Performance Goals**: Keep semantic mode bounded by `--limit` for examples while computing accurate totals. Avoid additional MSBuild loads for duplicate project paths.  
**Constraints**: Default inspection must remain syntax-only and must not load MSBuild. Semantic paths remain repository-relative. Existing imports/design-time target execution warning remains visible. Do not infer runtime defects from compiler binding or diagnostics.

## Constitution Check

- **Evidence First**: Separates syntax expressions, bound method calls, unresolved calls, load failures, and compiler errors; reports symbol locations when source-backed.
- **Broad, Engine-Neutral Core**: C# and Visual Basic semantic results remain plain JSON and Markdown evidence for any SDD engine.
- **Deterministic and Reviewable Output**: Stable project and diagnostic ordering; samples bounded while totals remain accurate.
- **Safe Inspection by Default**: Only explicit `--semantic` loads MSBuild. Every discovered .NET project is attempted once, while reports remain scoped to repository-owned project/source paths.
- **Specified and Verified Changes**: Fixture tests cover count arithmetic, path-safe targets, unresolved reasons, standalone projects, duplicate solutions, and partial loading.

All gates pass. No constitution exception is required.

## Project Structure

```text
specs/033-semantic-resolution-coverage/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/semantic-resolution-coverage.md
└── tasks.md

src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs
src/BrownPants.Cli/Analysis/RepositoryInventory.cs
src/BrownPants.Cli/Output/MarkdownReport.cs
src/BrownPants.Cli/Program.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
README.md
```

**Structure Decision**: Extend the existing semantic analyzer and report model. Keep MSBuild project loading, semantic count calculation, and text rendering in their existing layers; add no separate package or service.

## Design

The repository scanner continues to provide a fast syntax-only inventory unless `--semantic` is selected. Semantic loading opens one stable, alphabetically first discovered solution to establish project context, then opens discovered C#/VB projects not represented by it. A normalized project-path set prevents the same project from being analyzed more than once. Other discovered solutions do not cause duplicate evaluations; projects discovered in them are still opened directly if absent from the selected solution. A failed solution remains an issue even if direct project fallback succeeds.

For every repository-contained syntax tree, the analyzer counts invocation syntax nodes, intentionally skipped forms such as C# `nameof`, analyzed calls, and resolved/unresolved methods. It gathers compiler error totals and bounded diagnostics from each loaded compilation. Workspace/project load issues retain a total count separate from their bounded samples. Unresolved calls are grouped by `CandidateReason`; `None` is reported as the compiler supplying no candidate reason, not as a defect classification. Project coverage reflects projects not loaded and workspace/project load issues; unresolved calls and compiler errors remain separate evidence. MSBuild may return a project object despite malformed metadata, so successful object/compilation loading does not imply clean project evaluation.

Markdown will label the semantic section as .NET C#/VB method binding, reconcile the counts, and show target source locations when they are inside the repository. JSON receives additive fields under its existing semantic object; schema version remains unchanged. External and metadata targets keep null repository locations.

## Verification

Use focused semantic tests for C# and Visual Basic count reconciliation, `nameof` skip counts, target paths, unresolved candidates, compiler errors, sample truncation, solution deduplication, standalone projects, and failed projects. Verify Markdown/JSON parity and default mode behavior. Run the full Release suite, acceptance digest/evidence validators, semantic self-inspection, adapter checks, and packed CLI smoke check.

## Complexity Tracking

No constitution violations. The loading fallback and path deduplication are necessary to make the loaded-project counts correspond to all discovered project files without double-counting solutions.
