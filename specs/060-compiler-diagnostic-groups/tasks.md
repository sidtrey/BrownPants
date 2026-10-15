# Tasks: Compiler Diagnostic Groups

**Input**: Design documents in `specs/060-compiler-diagnostic-groups/`

## Phase 1: User Story 1 - Triage repeated compiler errors (P1)

**Independent test**: Construct multiple Roslyn compilations with repeated error codes in multiple project paths. Confirm exact project/code group counts, deterministic ordering regardless of compilation order, separate bounded sample and omission counts, and unchanged raw diagnostic totals.

- [x] T001 [US1] Add fixture tests for multi-project compiler diagnostic grouping, order stability, and independent group/raw sample limits in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T002 [US1] Aggregate compiler errors by normalized project path and diagnostic code, add group count/sample fields, and preserve existing raw diagnostic fields in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`.
- [x] T003 [US1] Render bounded compiler diagnostic groups in Markdown and verify JSON additive fields and raw source-located samples in `src/BrownPants.Cli/Output/MarkdownReport.cs` and `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T004 [US1] Document grouping limits, ignore generated/user-specific .NET artifacts, run build/tests and a published-CLI smoke, validate acceptance evidence, and record BrownPants-on-BrownPants results in `.gitignore`, `README.md`, and `specs/060-compiler-diagnostic-groups/verification.md`.

## Dependencies & Execution Order

- T001 establishes the observable behavior and expected failure before implementation.
- T002 depends on T001 and provides the aggregate data.
- T003 depends on T002 and verifies both public report formats.
- T004 runs after focused and full checks pass.

## Implementation Strategy

Complete the P1 triage flow as one increment: test aggregate semantics, implement exact aggregation with bounded output samples, expose both JSON and Markdown summaries, then validate against the known BrownPants self-report. Do not suppress or relabel individual diagnostics as causes.
