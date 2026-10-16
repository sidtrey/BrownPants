# Tasks: JavaScript and TypeScript Source Evidence

**Input**: Design artifacts in this feature directory.

**Tests**: Required. Add fixture-backed xUnit tests before changing analysis behavior.

## Phase 1: User Story 1 - Understand JS/TS declarations and call paths (Priority: P1)

**Independent Test**: A temporary monorepo produces expected source records and relationships in stable JSON/Markdown; unsafe, dynamic, malformed, and ambiguous inputs remain bounded and labeled.

- [x] T001 [US1] Add JSON/Markdown record models and deterministic rendering contract with tests for paths, lines, totals, limits, parity, and unchanged schema version. (AC-001, AC-005, FR-006)
- [x] T002 [US1] Add bounded JS/TS tokenizer with source offsets and tests for comments, all string forms, regex literals, templates, JSX text, CRLF/LF, and malformed input. (AC-001, AC-003, AC-005, SC-001, SC-003)
- [x] T003 [US1] Parse declaration, import/export, and call candidates for all supported source extensions; cover generics, overload-like names, barrel re-exports, JSX/TSX, and exact source locations. (AC-001, AC-003, FR-001, FR-002, SC-001)
- [x] T004 [US1] Resolve relative modules, index files, package workspaces, and configured tsconfig aliases under the root; test valid, missing, cyclic, malformed, escaping, and wildcard cases. (AC-002, AC-004, FR-003, FR-005, SC-002)
- [x] T005 [US1] Build bounded syntax call relationships; distinguish unique syntax targets from ambiguous/unresolved cases including dynamic imports, external packages, and runtime dispatch. (AC-003, AC-004, FR-004, FR-005, SC-002)
- [x] T006 [US1] Integrate analysis into inventory JSON/Markdown, language coverage, and README; preserve feature 014 factory/DI signals and add regression tests. (AC-005, FR-006, FR-007, FR-008, SC-004)
- [ ] T007 [US1] Run focused/full tests, Release build, adapter checks, package/install smoke tests, self-inspection, hash/ledger checks; record evidence and reconcile gaps. (SC-005)

## Dependencies

- T002 precedes T003 and T005.
- T003 and T004 precede T005.
- T001 and T005 precede T006.
- T007 follows all implementation tasks.
