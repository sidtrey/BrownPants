# Tasks: Semantic Source Path Boundaries

**Input**: Design documents from `/specs/027-semantic-path-boundaries/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/semantic-path-boundary.md`

**Tests**: Required by the acceptance criteria and project constitution. Add fixture-based xUnit coverage before changing analyzer behavior.

## Phase 1: User Story 1 - Keep semantic reports repository-scoped (Priority: P1)

**Goal**: Semantic findings contain repository-owned source locations and do not expose paths from local package caches or external source trees.

**Independent Test**: Analyze one repository source tree that calls a method in an external source tree; verify only repository declarations and call sites are reported and the call remains resolved without external target location.

### Tests for User Story 1

- [x] T001 Add Roslyn fixture tests for external syntax trees, repository-only counts, safe normalized paths, and external target path omission in `tests/BrownPants.Tests/RepositoryScannerTests.cs`

### Implementation for User Story 1

- [x] T002 Add repository path containment and filtering to semantic analysis in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`
- [x] T003 Run the full Release build/test suite, semantic self-scan in JSON and Markdown, and acceptance checks; record observed results in `specs/027-semantic-path-boundaries/verification.md`

## Dependencies & Execution Order

- T001 precedes T002. T003 is the final verification gate.

## Implementation Strategy

Prove the external-source leak with a deterministic temporary compilation, then filter semantic findings by the inspected root while preserving external symbol binding. Finally verify compatibility through repository self-analysis and the existing suite.
