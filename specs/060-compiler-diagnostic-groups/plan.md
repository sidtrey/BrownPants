# Implementation Plan: Compiler Diagnostic Groups

**Branch**: `060-compiler-diagnostic-groups` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**Input**: BrownPants-on-BrownPants semantic compiler errors are currently presented only as individual samples.

## Summary

Aggregate compiler errors by normalized project path and diagnostic code during semantic analysis. Expose bounded, deterministically sorted group samples and exact group counts alongside unchanged raw diagnostic samples, then render the distribution in Markdown without asserting causes.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing Roslyn compilation diagnostics; no new packages
**Storage**: None
**Testing**: xUnit fixtures that construct Roslyn compilations and inspect Markdown/JSON output
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local repository-analysis CLI
**Performance Goals**: One linear aggregation pass over compiler errors; group samples bounded by the user limit
**Constraints**: Keep compiler error totals and raw samples unchanged; output stable project-relative paths; no causal classification
**Scale/Scope**: Semantic C# and Visual Basic project compilations already loaded by the CLI

## Constitution Check

- **Evidence First**: Pass. Group keys and counts come directly from Roslyn errors; summaries do not assert root causes.
- **Broad, Engine-Neutral Core**: Pass. The summary is in the shared semantic report and independent of SDD adapters.
- **Deterministic and Reviewable Output**: Pass. Group samples are sorted and bounded; raw source-located diagnostics remain available.
- **Safe Inspection by Default**: Pass. This changes opt-in semantic output only; default inventory remains non-evaluating.
- **Specified and Verified Changes**: Pass. The spec covers grouping, exact omitted counts, Markdown/JSON, and self-analysis.

## Research and Design Decisions

- `CSharpSemanticAnalyzer.AnalyzeCompilations` already visits every error in the loaded C# and Visual Basic project compilations, counts all errors, and stores only a bounded set of raw rows. Aggregate group counts in the same pass so they are independent of raw sample truncation.
- Use the normalized repository-relative project path and Roslyn diagnostic ID as the key. Do not group by message: messages can vary within a code and are not a stable group identity.
- Retain all aggregate counts internally, but sort and cap public group rows using the existing `--limit`. Report exact group total and omissions separately from raw compiler-error sample omissions.
- Add fields to the existing semantic JSON shape without changing `SchemaVersion`; render group counts in Markdown and keep individual rows.
- Counts are descriptive only. Do not label groups as causes, prescribe fixes, or suppress source-located errors.

## Project Structure

```text
src/BrownPants.Cli/Analysis/RepositoryInventory.cs
src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs
src/BrownPants.Cli/Output/MarkdownReport.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
specs/060-compiler-diagnostic-groups/
```

**Structure Decision**: Extend the existing semantic report record and analyzer, render additive fields in Markdown, and cover behavior in the current test suite. No new project or external dependency is needed.

## Risks and Mitigations

- Different messages can share a code: the group contains counts only and raw message samples remain available.
- Large repositories can have many groups: output samples are bounded by `--limit`, with exact total and omitted counts; aggregation keeps one counter per distinct project/code pair.
- Consumers may rely on the schema: retain version 1 and add fields without changing existing names or values.

## Complexity Tracking

No constitution violations or new runtime dependencies.
