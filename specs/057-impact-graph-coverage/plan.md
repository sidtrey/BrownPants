# Implementation Plan: Impact Graph Coverage

**Branch**: `057-impact-graph-coverage` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Keep the public semantic call sample bounded by `--limit`, while separately retaining a bounded graph for managed impact traversal. Impact uses the internal graph when available and reports its own truncation; serialized semantic inventory remains unchanged.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing Roslyn C# and VB semantic analysis
**Storage**: None
**Testing**: xUnit compilation fixtures and installed-tool impact smoke
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local CLI analyzer
**Performance Goals**: Linear semantic edge capture up to a fixed, explicit ceiling; bounded graph traversal
**Constraints**: Do not serialize the internal graph; preserve public `--limit` behavior; no new package or project-evaluation behavior
**Scale/Scope**: Resolved invocation and constructor edges already produced by MSBuildWorkspace

## Constitution Check

- **Evidence First**: Pass. Graph omissions, unresolved calls, and workspace issues remain separate completeness reasons.
- **Broad, Engine-Neutral Core**: Pass. The change is in the semantic analysis and impact core, not an SDD adapter.
- **Deterministic and Reviewable Output**: Pass. Retention order remains deterministic; the internal graph is bounded and reports exact omission counts.
- **Safe Inspection by Default**: Pass. Only the existing explicit semantic/impact path loads MSBuild; default inventory does not change.
- **Specified and Verified Changes**: Pass. Frozen criteria, fixture coverage, package smoke, and self-analysis verification are planned.

## Research and Design Decisions

- `CSharpSemanticAnalyzer.AnalyzeCompilations` currently adds resolved invocation and constructor edges to one list through `AddSample(..., sampleLimit)`. `ResolvedInvocationCount` and `ResolvedConstructorCount` retain full counts, but the call edges beyond `--limit` are discarded.
- `CallImpactAnalyzer.Analyze` currently traverses `semantic.ResolvedCalls`, the same display sample. This makes project/tree enumeration order affect which changed-file callers can be found.
- The pre-implementation BrownPants-on-BrownPants baseline loaded 5 of 5 projects and retained all 817 declarations, but the root had 11,084 resolved relationships. At `--limit 10,000`, the changed Bug Lab workflow had 0 retained matching calls and 0 managed impact findings; analyzing the Bug Lab as its own root produced 10 managed findings. This confirmed the sample-order gap; post-implementation measurements are in [verification.md](verification.md).
- Add an internal impact-edge list with a default ceiling of 100,000 edges. Capture each resolved invocation or constructor into the public sample and the internal list independently. Count every edge omitted after the internal ceiling.
- Add an internal test overload for a smaller graph ceiling so truncation can be verified without generating 100,000 syntax nodes. Tests receive internal access only; the CLI does not expose this implementation setting.
- `CallImpactAnalyzer` uses the internal graph when populated; hand-built semantic fixtures without one continue to use `ResolvedCalls`. Completeness compares total resolved edges against the internal retained count and omitted count, not against the public display sample.
- Keep `CSharpSemanticAnalysis` internal graph fields non-public so JSON shape and public schema remain unchanged.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── CSharpSemanticAnalyzer.cs # retain public samples and bounded internal graph
├── RepositoryInventory.cs   # internal graph fields and omission count
└── CallImpactAnalyzer.cs    # traverse internal graph and report graph truncation
tests/BrownPants.Tests/
├── CallImpactAnalyzerTests.cs
└── RepositoryScannerTests.cs
tests/PackedImpactGraphCoverageSmoke.ps1
specs/057-impact-graph-coverage/
```

**Structure Decision**: Reuse existing semantic analysis, report models, and CLI; add no new runtime service or package.

## Risks and Mitigations

- The internal graph consumes more memory than the public sample. Keep a fixed ceiling and report the exact omitted count.
- Projects after the internal ceiling may still contain callers. Mark the report incomplete and do not state that no callers exist.
- The existing public semantic JSON can be consumed by tools. Keep its fields and `--limit` sample unchanged and test serialization.
- A fully retained graph can still be semantically incomplete due to unresolved calls or workspace issues. Preserve those diagnostics independently.

## Complexity Tracking

No constitution violations. The extra internal bounded list is required because impact needs relationships that the public display sample intentionally omits; the cap keeps memory reviewable.
