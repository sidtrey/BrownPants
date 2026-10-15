# Implementation Plan: Focused Evidence Handoff

**Branch**: `052-focused-evidence-handoff` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Extend `evidence` with optional task-local selection using literal clues and changed paths. Reuse `RepositoryInvestigator` for bounded clue matches and `CallImpactAnalyzer` for compiler-bound direct/transitive callers. Keep the current unfiltered schema output byte-stable when focus options are absent. Focused output will explain which input selected each finding, show unmatched inputs and counts, and keep evidence status/limits intact.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing Roslyn and Microsoft.Build.Locator packages; no new package
**Storage**: None
**Testing**: xUnit in `tests/BrownPants.Tests`; temporary repository fixtures
**Target Platform**: Windows, Linux, macOS .NET 10 CLI
**Project Type**: Local command-line application
**Performance Goals**: Reuse the single repository scan and bound matches/findings by `--limit`; avoid repeated full source reads per clue
**Constraints**: Semantic loading remains explicit with `--semantic`; paths stay under the repository root; output is deterministic and does not execute source/tests or use network access
**Scale/Scope**: One evidence report, multiple bounded clue/path selectors, existing sample limit 1–10000

## Constitution Check

- **I. Evidence First**: Preserved by retaining finding status and adding selection reasons; clue/path matches remain leads, not causality.
- **II. Broad, Engine-Neutral Core**: Preserved through JSON fields usable by any SDD engine; selectors do not require Spec Kit.
- **III. Deterministic and Reviewable Output**: Preserved through stable selector/finding ordering, bounded results, and unchanged no-focus output.
- **IV. Safe Inspection by Default**: Preserved; clue-only and changed-path candidate selection do not execute code. Resolved caller analysis runs only with `--semantic`.
- **V. Specified and Verified Changes**: Hashed criteria, concrete tasks, fixtures, and project validation are required before completion.

No constitution violations identified.

## Design

### Phase 0: Research

- Reuse `RepositoryInvestigator.Investigate` rather than implementing a second clue matcher. It already bounds reads and reports literal path/content match locations without excerpts.
- Reuse `CallImpactAnalyzer.Analyze` for changed C#/VB files; it validates repository-relative paths and follows compiler-resolved callers transitively.
- Keep `EvidenceReportAnalyzer` responsible for full-report finding identities. Add a focused selection layer that associates those findings with clue match locations and impact edges, de-duplicates findings, and records reasons.
- Keep the full report's serialization unchanged by omitting optional focus fields when no focus inputs were supplied.

### Phase 1: Data and contract

The focus request has repeatable clues, repeatable changed paths, the existing semantic flag, and the existing limit. A focused response adds a nullable focus summary and optional per-finding `matchedBy` references. The summary reports each selector's matches, unmatched selectors, whether semantic impact ran, total selected findings, emitted findings, and omissions. Exact JSON details are in [contracts/focused-evidence.json.md](contracts/focused-evidence.json.md).

### Error behavior

Reject empty clues, absolute/traversing/missing paths, and unsupported changed-path extensions using existing CLI exit-code conventions. Clues and changed paths form a deterministic union; one finding appears once, with all reasons that selected it. A changed C#/VB path without `--semantic` selects source-local candidates and explicitly reports that caller edges were not analyzed.

## Project Structure

```text
src/BrownPants.Cli/
├── EvidenceCommand.cs                       # parse focus options and serialize result
└── Analysis/
    ├── EvidenceReport.cs                    # optional focused metadata and findings
    ├── EvidenceFocusAnalyzer.cs              # union selection and matched-by reasons
    ├── RepositoryInvestigator.cs             # existing bounded clue matches
    └── CallImpactAnalyzer.cs                 # existing semantic change impact
tests/BrownPants.Tests/
└── EvidenceCommandTests.cs                  # full-mode compatibility and focused fixtures
specs/052-focused-evidence-handoff/
├── contracts/focused-evidence.json.md
├── data-model.md
└── quickstart.md
```

**Structure Decision**: Extend the existing CLI and analysis layer; no new assembly or dependency is justified.

## Complexity Tracking

None.
