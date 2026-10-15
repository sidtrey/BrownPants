# Research: SDD Evidence Handoff

## Existing evidence sources

`RepositoryScanner.Scan(root, sampleLimit, analyzeSemantics)` already produces a `RepositoryInventory` with project systems, language coverage, test paths, architecture signals, parsed source structure, and optional semantic analysis. Semantic analysis separates `ResolvedCalls` from `UnresolvedCalls` and records project coverage and diagnostics. The inventory serializer is deterministic by the analyzer's ordering and includes a schema version.

## Gap from the measured run

The prior assisted result improved recall by only five points and had lower precision than baseline. Its structured result missed factory/DI signals and the test location, miscategorized a C++/CLI candidate, and added unsupported findings. Existing outputs require an agent to manually translate Markdown/JSON evidence into the result format, increasing opportunities for omission and label drift.

## Decision

Add a separate task-neutral JSON projection command using only current analysis results. Do not change existing report contracts or the frozen evaluation rubric. Preserve candidates as candidates and emit managed edges only from resolved Roslyn calls. The feature is successful only when a subsequent paired run meets the frozen thresholds; generating more deterministic evidence alone is not proof that agents perform better.

## Open questions

None. The existing scanner provides the needed source facts and explicit semantic status.
