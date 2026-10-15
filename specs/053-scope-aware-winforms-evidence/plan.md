# Implementation Plan: Scope-Aware WinForms Control Evidence

**Branch**: `053-scope-aware-winforms-evidence` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Replace per-file flat name maps with a two-pass source index for C# and Visual Basic. The first pass records namespaces, imports/aliases, control inheritance, and declared field/property types across files. The second resolves assignment receivers using their lexical scope and indexed member types. Keep the existing offline syntax-only contract and report unresolved evidence as coverage gaps.

## Technical Context

**Language/Version**: C# / .NET 10; Roslyn C# and Visual Basic syntax APIs
**Primary Dependencies**: Existing compiler packages only
**Storage**: None
**Testing**: xUnit temporary repositories; packed CLI smoke
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local CLI analyzer
**Performance Goals**: O(files + declarations + assignments) indexing/resolution; retain bounded report output
**Constraints**: No MSBuild evaluation, build, code execution, network, repository writes, source excerpts, or new dependencies
**Scope**: Source-visible partial/derived types, aliases, fields/properties, locals/parameters, and member chains in C# and VB; ambiguous/dynamic/reflection-only receivers remain unresolved

## Constitution Check

- **Evidence First**: Findings need a declared type path to a recognized WinForms Control; ambiguous cases remain out of the positive findings and are summarized as issues.
- **Broad, Engine-Neutral Core**: The existing `winforms analyze` command remains independent of any SDD host.
- **Deterministic and Reviewable Output**: Stable source paths, locations, issue samples, result limits, and JSON/Markdown.
- **Safe Inspection by Default**: Source parsing only; no MSBuildWorkspace or repository execution.
- **Specified and Verified Changes**: Criteria are frozen and each task links to criteria plus observed verification evidence.

Gate: PASS. The analyzer still cannot prove runtime types, active preprocessor configurations, thread identity, or safe marshaling.

## Research Decisions

See [research.md](research.md). The current `WinFormsUiThreadingAnalyzer` indexes one file at a time and stores all local declarations in a single name-to-type dictionary. That makes cross-file declarations invisible and permits same-name declarations to overwrite each other. Reuse Roslyn syntax parsing and add explicit namespace-aware type identities and scope-bound local lookup. Do not use semantic project loading because this command promises safe source-only analysis.

## Architecture

1. Discover and parse C#/VB sources using existing repository discovery and language-version metadata.
2. Create immutable per-file syntax contexts containing path, root, namespace/import/alias data, declarations, and diagnostics.
3. Index source types by namespace-qualified identity, combining partial declarations; iterate inheritance until the local source-derived Control set stabilizes.
4. Index source member (field/property) declared types per containing type. Resolve identifiers against nearest visible local/parameter declaration first, then instance members; resolve member chains one declared type at a time.
5. For VB, honor case-insensitive names and block/method/type boundaries. For C#, honor case-sensitive identifiers, lexical block boundaries, and nearest declaration before the assignment.
6. Emit candidate findings only for positive type evidence. Count and sample unresolved assignment targets that are plausible control receivers, while avoiding unbounded issue output.
7. Preserve existing background and marshaling observations, report ordering, and CLI. Add coverage counts for unresolved receiver targets without claiming the report lists all possible UI writes.

## Contracts and Data

See [data-model.md](data-model.md) and [contracts/winforms-report.md](contracts/winforms-report.md). New coverage metadata is additive. Existing candidate fields and default source-only behavior remain stable.

## Validation

Run focused WinForms tests, full solution tests, Release build, and packed CLI smoke against a legacy .NET Framework fixture. Compare a frozen expected path/property set, including same-name decoys. Verify the repository remains unchanged by the analyzer.

## Project Structure

```text
src/BrownPants.Cli/Analysis/WinFormsUiThreadingAnalyzer.cs
src/BrownPants.Cli/Analysis/WinFormsUiThreadingModels.cs
src/BrownPants.Cli/Output/WinFormsUiThreadingReportRenderer.cs
tests/BrownPants.Tests/WinFormsUiThreadingTests.cs
tests/PackedWinFormsSmoke.ps1
specs/053-scope-aware-winforms-evidence/
```

## Complexity Tracking

No constitution violations. Indexing and scope resolution are syntax approximations and must expose uncertainty rather than imply compiler binding.
