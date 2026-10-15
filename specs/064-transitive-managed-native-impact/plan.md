# Implementation Plan: Transitive Managed-to-Native Impact

**Branch**: `064-transitive-managed-native-impact` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Seed native reverse tracing from exact declarations of impacted managed callers. Preserve native edge depth and add the managed caller depth that led to each seed, so a repository-file change can be followed through C++/CLI and MFC without selecting unrelated same-file methods.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing Roslyn and native lexical analyzers; no new packages
**Storage**: N/A
**Testing**: xUnit in `tests/BrownPants.Tests`
**Target Platform**: .NET 10 supported platforms (Windows, Linux, macOS)
**Project Type**: CLI analyzer
**Performance Goals**: Reuse bounded graphs and indexes already produced by one scan; no per-edge repository rescans
**Constraints**: Offline, read-only, deterministic; no MSBuild evaluation for default analysis; candidate edges remain lexical
**Scale/Scope**: One call-impact invocation, bounded by existing semantic/native graph sampling and requested sample limit

## Constitution Check

- **Evidence First**: Managed-to-native seeds require exact declaration locations; outputs retain native candidate status and state limits.
- **Broad, Engine-Neutral Core**: Change is in core call-impact and evidence output, independent of Spec Kit or any host.
- **Deterministic and Reviewable Output**: Sort seeds/results and use normalized repository-relative paths.
- **Safe Inspection by Default**: Consume already collected source analysis; do not evaluate projects or write to inspected repositories.
- **Specified and Verified Changes**: Acceptance hash, regression fixtures, evidence ledger, full build and test suite required.

All gates pass; no constitution exception is needed.

## Project Structure

```text
src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs
src/BrownPants.Cli/Output/CallImpactReportRenderer.cs
tests/BrownPants.Tests/CallImpactAnalyzerTests.cs
tests/BrownPants.Tests/EvidenceCommandTests.cs
examples/brownfield-bug-lab/
specs/064-transitive-managed-native-impact/
```

**Structure Decision**: Extend the existing analyzer/model/renderer and their unit/CLI integration tests; no new project or dependency.

## Design

For each changed managed file, run the existing resolved caller traversal and retain exact declarations for changed symbols and impacted caller symbols. Pair a native trace seed with its managed depth only when a native relation's resolved target path and line, or one of its candidate target locations, matches that declaration. Direct path seeds remain unchanged. Continue existing native upstream traversal by symbol. Deduplicate by changed path and relation identity, selecting the shallowest deterministic managed depth when multiple paths reach the same relation.

Add an optional managed-depth field to native impact records; null means a direct path seed. Keep existing native `Depth` values unchanged. Render both depth dimensions when present. Evidence focus already consumes the resulting native impact records and should need no selection-rule change; integration coverage must prove exact findings are surfaced.

## Risks

- Declaration line identity can collide for multiple methods on one line. Where symbol identity and relation target symbol are available, require both along with path/line; do not rely on line alone.
- Sampling or absent declarations can suppress a seed; preserve and report existing completeness issues.
- A relation with only a file path and no exact target location is not sufficient for a managed-derived seed.
