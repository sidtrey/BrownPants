# Implementation Plan: SDD Evidence Handoff

## Summary

Add `brownpants evidence <repository> --format json` as a deterministic, SDD-ready projection of existing inspection data. It emits normalized findings with candidate/resolved status, evidence paths/lines, managed caller/target signatures, test paths, project context, and analysis limitations. It does not infer benchmark-specific IDs or use an answer key. Semantic project loading is opt-in with `--semantic` and must explain MSBuild evaluation risk.

## Technical Context

- CLI: .NET 10, C# nullable enabled, existing `System.Text.Json` serialization.
- Analysis: `RepositoryScanner.Scan` supplies project/language inventory, architecture signals, test paths, and optional Roslyn semantic edges.
- Tests: xUnit in `tests/BrownPants.Tests`; command tests can invoke `Program.Run` against temporary repositories.
- Dependencies: no new NuGet packages.
- Output: versioned JSON contract, deterministic ordering and duplicate removal, repository-relative paths.

## Repository Evidence

- `src/BrownPants.Cli/Program.cs` dispatches inspect/investigate/impact and serializes records as JSON.
- `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` defines inventory, architecture signals, discovered test paths, and resolved/unresolved semantic call records.
- `tests/BrownPants.Tests/RepositoryScannerTests.cs` covers CLI invocation and temporary repository fixtures.
- `examples/brownfield-bug-lab/evaluation/` freezes the paired evaluation task and answer key. The evidence command must remain task-neutral and must not read those evaluation artifacts.
- `docs/comparisons/brownfield-bug-lab-2026-10-results.json` records the prior baseline at 5/20 TP, 2 FP (71.4% precision, 25% recall) and assisted run at 6/20 TP, 3 FP (66.7%, 30%).

## Constitution Check

- Evidence First: preserve `candidate` versus `resolved`; unresolved edges and limits remain visible.
- Broad, Engine-Neutral Core: output is generic JSON for any SDD engine.
- Deterministic and Reviewable: stable ordering, relative paths, bounded output, evidence text.
- Safe Inspection: default remains static; semantic MSBuild loading requires explicit opt-in.
- Specified and Verified Changes: task-to-criterion evidence ledger and frozen hash gate apply.

## Design

### Report model

Top-level `schemaVersion`, `root`, `context`, `findings`, `limitations`, and `completeness`. Each finding contains deterministic `id`, `category`, `signal`, `status`, `path`, optional `line`, optional `callerSignature`/`targetSignature`, and concise `evidence`. IDs derive from normalized finding identity, not content outside the repository.

Resolved call findings come only from Roslyn resolved calls. Architecture/framework signals and test paths are candidates. Unresolved calls are summarized in limitations, not emitted as resolved findings. C++/CLI, MFC, COM, and NHibernate signals map to framework candidates based on analyzer category/evidence; all remaining architecture signals retain architecture candidate status.

### Command contract

`brownpants evidence [repository] [--format json] [--limit 1..10000] [--semantic]`

JSON is the initial supported format. Invalid options return usage errors. Default analysis does not load projects. `--semantic` follows inspect semantic-mode behavior and records workspace/project coverage issues. Output uses a new schema version independent of inspect's schema.

## Risks and Mitigations

- Semantic loading can evaluate imported targets: require explicit `--semantic` and document it.
- Large repositories can produce large reports: honor the existing sample limit and report omitted totals/completeness.
- Category mapping could overstate framework semantics: map only explicit recognized signal families; keep every item candidate.
- A stronger evidence handoff may not improve model-written results: retain the frozen head-to-head gate and publish actual outcome.

## Validation

Build and test the solution. Add temporary-repository tests for stable JSON, resolved versus candidate labels, duplicate identities, ignored decoys through analyzer behavior, limits, invalid options, semantic opt-in, and limitations. Run the BrownPants fixture export and score the export as a diagnostic against the frozen answer key, then run the matched agent protocol only if the required model/host is available; never substitute deterministic export scoring for the agent comparison.
