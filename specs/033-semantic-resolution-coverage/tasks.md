# Tasks: Semantic Resolution Coverage

**Input**: [spec.md](spec.md), [plan.md](plan.md), and the design artifacts in this feature directory.
**User Story**: US1 is the single P1 story. Fixture tests are required by the spec and constitution.

## Phase 1: User Story 1 — Understand call-resolution evidence (P1)

**Goal**: Reconcile semantic counts, navigate resolved targets, and identify incomplete project loads without overstating defects.

**Independent Test**: Temporary C#/VB compilations and MSBuild projects verify report count identities, target locations, unresolved reasons, project deduplication/fallback, issue and compiler-error totals, and Markdown/JSON parity.

- [X] T001 [US1] Add fixture tests for semantic counts, `nameof` skips, resolved target locations, unresolved reasons, issue truncation, compiler diagnostics, standalone projects, duplicate solution membership, and failed projects in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T002 [US1] Extend semantic result models and analyzer aggregation for invocation counts, skipped forms, unresolved reason totals, workspace issue totals, compiler errors, and bounded diagnostic samples in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`.
- [X] T003 [US1] Load discovered `.csproj` and `.vbproj` files omitted from solutions; deduplicate normalized project paths across solutions; count failed projects and retain load issues in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`.
- [X] T004 [US1] Render reconciled totals, unresolved-reason counts, project coverage, diagnostic totals, and repository target paths in `src/BrownPants.Cli/Output/MarkdownReport.cs`; keep JSON and Markdown facts aligned.
- [X] T005 [US1] Clarify syntax-only versus semantic behavior, target navigation, coverage limits, and opt-in MSBuild risk in `Program.cs`, `README.md`, and `specs/033-semantic-resolution-coverage/quickstart.md`.
- [X] T006 [US1] Run focused and full Release tests, build, acceptance hash/evidence checks, integration scripts, and semantic self-inspection; record observed results in `specs/033-semantic-resolution-coverage/verification.md`.

## Dependencies & Execution Order

- T001 precedes T002–T005 and is expected to fail against the current behavior.
- T002 precedes T003 and T004 because project loading and rendering consume the expanded result model.
- T003 may proceed after the shared model is defined; it touches the existing analyzer.
- T004 and T005 can proceed after model fields are stable.
- T006 is the final validation gate.

## Implementation Strategy

Prove the report contracts with fixtures first. Add aggregate counts and diagnostics without changing existing fields, then load standalone projects with deterministic deduplication. Render equivalent Markdown/JSON evidence, document the explicit semantic boundary, and finish with cross-artifact and integration verification.
