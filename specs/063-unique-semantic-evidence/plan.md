# Implementation Plan: Unique Semantic Evidence

**Branch**: `063-unique-semantic-evidence` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Preserve every distinct Roslyn-resolved relationship in evidence reports. Carry each call's one-based source column into the evidence model, preserve existing IDs when unique, and deterministically derive a new ID only when source findings collide. Build full and focused output from the finalized unique set.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing Roslyn semantic analysis, System.Text.Json, xUnit
**Storage**: None
**Testing**: BrownPants.Tests evidence command fixtures; Bug Lab JSON smoke
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local repository-analysis CLI
**Performance Goals**: Linear pass over the existing bounded finding sample; no extra repository scan
**Constraints**: Keep schema version 1, stable IDs for non-colliding findings, deterministic collision handling, exact one-based source columns
**Scale/Scope**: Resolved managed invocations/constructors represented in evidence reports and existing candidate rows whose IDs collide.

## Constitution Check

- **Evidence First**: Pass. Columns and expression rows point to Roslyn source locations; no new relationship is inferred.
- **Broad, Engine-Neutral Core**: Pass. The additive JSON field is consumable by any SDD host.
- **Deterministic and Reviewable Output**: Pass. IDs and rows are finalized in stable order before grouping and filtering.
- **Safe Inspection by Default**: Pass. This changes report assembly only; semantic MSBuild loading stays opt-in.
- **Specified and Verified Changes**: Pass. Regression fixtures prove full and focused behavior; the frozen acceptance hash and evidence ledger remain in force.

## Research and Design Decisions

- `CSharpSemanticAnalyzer` records line, expression, caller, and target for resolved methods and constructors, but not the source column.
- `EvidenceReportAnalyzer.CreateFinding` hashes category/status/signal/path/line/caller/target, then applies `DistinctBy(Id)`. Two constructor expressions on the same line that resolve to the same constructor therefore collide and one is silently discarded.
- Focused reports map findings by ID with `ToDictionary`; duplicate IDs would also make that path throw if they were merely retained.
- Add an optional source column to `CSharpResolvedCall` and `EvidenceFinding`. Set it only for managed-call evidence, preserving existing candidate JSON shapes.
- Keep the first ID for every existing base identity. On collision, derive a deterministic secondary ID using the base ID, source column when available, evidence text, and a stable ordinal. Sort ties by evidence before finalization. Do not `DistinctBy` and discard valid source occurrences.
- Derive total/emitted counts and repeated candidate groups after ID finalization so all report consumers see the same canonical finding set.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── RepositoryInventory.cs
├── CSharpSemanticAnalyzer.cs
├── EvidenceReport.cs
└── EvidenceFocusAnalyzer.cs
tests/BrownPants.Tests/EvidenceCommandTests.cs
specs/063-unique-semantic-evidence/
├── spec.md
├── plan.md
├── research.md
├── quickstart.md
├── tasks.md
└── verification.md
```

**Structure Decision**: Add location data to the existing semantic relationship record and expose it as an optional evidence property. Finalize identity once in the shared full-report builder so both full and focused reports use the same IDs.

## Risks and Mitigations

- Hash disambiguation could change a previously unique ID if applied indiscriminately; only collision members after the first receive derived IDs.
- Identical repeated expressions need a stable tie order; syntax traversal is deterministic and each resolved call carries its source column.
- JSON consumers may ignore the additive column; schema version stays 1 and existing fields retain their meaning.

## Complexity Tracking

No constitution violations or new dependencies.
