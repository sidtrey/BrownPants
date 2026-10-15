# Implementation Plan: Grouped Candidate Evidence

**Branch**: `061-grouped-candidate-evidence` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**Input**: The brownfield benchmark's reviewed key groups same-signal locations, but the structured handoff and result instructions enumerate each line separately.

## Summary

Add a bounded, additive roll-up for repeated architecture/framework candidate findings keyed by category, signal, and repository-relative path. Keep canonical per-line findings unchanged. Recompute roll-ups after focus filtering. Update the controlled benchmark result schema and scorer to accept exact `lines` arrays, then repeat the same seeded comparison.

## Technical Context

**Language/Version**: C# / .NET 10; PowerShell 7 comparison tooling
**Primary Dependencies**: Existing JSON evidence report, Roslyn/source analyzers, xUnit; no new packages
**Storage**: None
**Testing**: xUnit fixtures, evaluator self-test, controlled bug-lab comparison
**Target Platform**: Windows, Linux, macOS; benchmark runner uses Codex CLI where available
**Project Type**: Local repository-analysis CLI and comparison harness
**Performance Goals**: Group after the existing bounded finding sample; no additional source scans
**Constraints**: Preserve raw finding IDs, schema version 1, completeness and focus semantics; do not merge managed call edges, native relations, or test-path candidates
**Scale/Scope**: Repeated architecture/framework candidate locations in the evidence handoff and the existing fixed bug-lab evaluation

## Constitution Check

- **Evidence First**: Pass. A group only rolls up exact category/signal/path matches and links existing IDs; it does not infer relevance or causation.
- **Broad, Engine-Neutral Core**: Pass. Group data is part of the engine-neutral evidence JSON; adapters may consume it.
- **Deterministic and Reviewable Output**: Pass. Group and location ordering is ordinal/stable; input findings remain bounded and canonical.
- **Safe Inspection by Default**: Pass. The handoff remains local and read-only; no project evaluation or network behavior is added.
- **Specified and Verified Changes**: Pass. Tests cover grouping, focus selection, backwards-compatible rows, and frozen comparison scoring.

## Research and Design Decisions

- `EvidenceReportAnalyzer.Create` already creates stable per-location findings and caps the emitted array with `--limit`. Build repeated groups from this emitted array only; the group locations therefore cannot exceed the raw finding sample.
- Group only `architecture-candidate` and `framework-candidate` records with a non-null line and an exact `(category, signal, path)` key. Do not merge semantically resolved calls, native-flow relations, or test paths.
- Store existing finding IDs and line numbers in each roll-up so tools can correlate it with canonical evidence. Keep raw `findings` and their IDs unchanged.
- Rebuild roll-ups in `EvidenceFocusAnalyzer.Select` after selection and limiting; otherwise a focused report could carry group members that were not selected.
- Keep report SchemaVersion 1 because all additions are optional additive properties. Existing clients may ignore `repeatedCandidateGroups`.
- Update the comparison prompt/result schema to submit one finding per repeated group with exact `lines`; update the scorer so grouped lines match a source-audited, pre-frozen v4 key. Preserve the v3 record and the unchanged hashed task spec, then rerun both conditions on the same current source.

## Project Structure

```text
src/BrownPants.Cli/Analysis/EvidenceReport.cs
src/BrownPants.Cli/Analysis/EvidenceReportAnalyzer.cs
src/BrownPants.Cli/Analysis/EvidenceFocusAnalyzer.cs
tests/BrownPants.Tests/EvidenceCommandTests.cs
examples/brownfield-bug-lab/evaluation/task-prompt-v4.md
examples/brownfield-bug-lab/evaluation/result.schema.md
examples/brownfield-bug-lab/evaluation/Score-Comparison.ps1
docs/comparisons/brownfield-evidence-grouping-2026-10.md
```

**Structure Decision**: Add a compact report model beside the existing evidence contract, a shared deterministic grouping function used by full and focused reports, tests in the evidence test suite, and a new versioned comparison prompt/result record. No new runtime project or package.

## Risks and Mitigations

- Group roll-ups could look like a behavioral relationship: label them as repeated source candidates and preserve raw evidence rows.
- The report may already be truncated: derive groups only from emitted rows and retain the existing completeness flag.
- The v3 benchmark key references a C++/CLI signal at line 2, while the current fixture and BrownPants output place it at lines 5, 10, and 11. Its recorded source commit is unavailable in local and public Git history. Preserve the v3 key; independently review and hash a v4 source-grounded key before running either agent. Keep the bug task and acceptance hash unchanged.
- One model pair is noisy: publish exact method, prompts, hashes, tokens, and limits; do not generalize beyond the measured pair.

## Complexity Tracking

No constitution violations or new runtime dependencies.
