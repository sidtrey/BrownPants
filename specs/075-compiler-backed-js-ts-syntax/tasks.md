# Tasks: Compiler-Backed JavaScript and TypeScript Syntax Checks

**Input**: Design documents from `specs/075-compiler-backed-js-ts-syntax/`

**Prerequisites**: `plan.md`, `spec.md`, and `research.md`

## Phase 1: Foundational model and parser adapter

**Purpose**: Define the verification evidence contract and safe local parser invocation.

- [x] T001 [US1] Add parser verification, compiler identity, per-file status, diagnostic, and heuristic-finding records in `src/BrownPants.Cli/Analysis/JavaScriptTypeScriptModels.cs`.
- [x] T002 [US1] Implement bounded local TypeScript package discovery, path/reparse checks, Node process invocation, timeout, and parser-only JSON exchange in `src/BrownPants.Cli/Analysis/TypeScriptSyntaxVerifier.cs`.
- [x] T003 [P] [US1] Add verifier tests for compiler discovery, nearest version, path containment, injected verifier result mapping, and deterministic output in `tests/BrownPants.Tests/TypeScriptSyntaxVerifierTests.cs`.

## Phase 2: User Story 1 - Verify source syntax with the local parser (Priority: P1)

**Goal**: An explicit inspect option reports compiler parser diagnostics and separates heuristic findings.

**Independent Test**: A local parser fixture recognizes valid JSX/TSX, nested templates, and regex syntax without warnings, and returns an exact code/message/location for malformed syntax.

- [x] T004 [US1] Integrate optional parser results into `JavaScriptTypeScriptAnalyzer.Analyze` and move verified files' lexical syntax warnings to heuristic findings in `src/BrownPants.Cli/Analysis/JavaScriptTypeScriptAnalyzer.cs`.
- [x] T005 [US1] Parse `--typescript-syntax`, pass it to repository scanning, and document the explicit parser-only/trust behavior in `src/BrownPants.Cli/Program.cs`.
- [x] T006 [US1] Render parser identity, verification status, exact diagnostics, and heuristic findings in `src/BrownPants.Cli/Output/MarkdownReport.cs` while preserving JSON parity through the inventory model.
- [x] T007 [US1] Add tests for syntax findings, Markdown/JSON formatting, and result mapping in `tests/BrownPants.Tests/TypeScriptSyntaxVerifierTests.cs`; verify actual parser diagnostics with a temporary malformed-source probe against FullMetalFile.

## Phase 3: User Story 2 - Preserve safe default behavior and explicit fallback (Priority: P1)

**Goal**: Default scans remain process-free; unavailable parser verification is explicit and keeps heuristic evidence.

**Independent Test**: Default scans never invoke the verifier; opt-in without Node or a local package yields a stable unavailable status and retains heuristic warnings.

- [x] T008 [US2] Add tests for default non-invocation, missing compiler, and fallback evidence in `tests/BrownPants.Tests/TypeScriptSyntaxVerifierTests.cs`; confirm explicit parser success on the FullMetalFile client.
- [x] T009 [US2] Update `README.md`, `AGENTS.md`, and inspect usage documentation with the new option, discovery rules, trust boundary, and syntax-only limits.

## Phase 4: User Story 3 - Recognize React projects and UI source (Priority: P1)

**Goal**: BrownPants identifies React package manifests and navigable component/hook syntax candidates.

**Independent Test**: A temporary React package and TSX component produce the manifest, component, and hook locations in the inventory and opt-in parser report.

- [x] T010 [US3] Detect bounded React dependencies in package manifests and add source-located React project models to `src/BrownPants.Cli/Analysis/ReactProjectAnalyzer.cs` and `src/BrownPants.Cli/Analysis/JavaScriptTypeScriptModels.cs`.
- [x] T011 [US3] Extract React function-component and hook-call candidates from parsed TSX ASTs in `src/BrownPants.Cli/Analysis/TypeScriptSyntaxVerifier.cs` and render them in `src/BrownPants.Cli/Output/MarkdownReport.cs`.
- [x] T012 [US3] Add React manifest, TSX component/hook location, and non-runtime-claim coverage in `tests/BrownPants.Tests/TypeScriptSyntaxVerifierTests.cs`.

## Phase 5: Cross-repository verification

**Purpose**: Prove the new report against FullMetalFile and run project quality gates.

- [x] T013 Run BrownPants build and unit suite, default inspect, explicit parser verification in Markdown and JSON, and the TypeScript parser comparison against FullMetalFile; record observed results in `specs/075-compiler-backed-js-ts-syntax/quickstart.md` and `acceptance-evidence.json`.
- [x] T014 Recompute the acceptance criteria digest and review task claims, evidence paths, output diffs, and repository worktree in `specs/075-compiler-backed-js-ts-syntax/acceptance-criteria.sha256` and `acceptance-evidence.json`.
- [x] T015 [US2] Label lexical parser-shape warnings as syntax heuristics in Markdown and JSON while retaining true analysis diagnostics as issues; add report contract coverage in `src/BrownPants.Cli/Analysis/JavaScriptTypeScriptModels.cs`, `src/BrownPants.Cli/Output/MarkdownReport.cs`, and `tests/BrownPants.Tests/RepositoryScannerTests.cs`. (AC-005, FR-008, FR-009, SC-004, SC-005)

## Dependencies

- T001 precedes T002-T008.
- T002-T003 precede T004 and T007-T008.
- T004 precedes T005-T007.
- T005-T009 precede T010-T012.
- T010-T012 precede T013-T014.

## Verification commands

- `dotnet build BrownPants.sln`
- `dotnet test BrownPants.sln`
- `dotnet run --project src/BrownPants.Cli -- inspect <repository> --typescript-syntax --format markdown --limit 1000`
- `dotnet run --project src/BrownPants.Cli -- inspect <repository> --typescript-syntax --format json --limit 1000`
- `node --check scripts/authz-matrix.mjs` from the FullMetalFile repository

## Implementation Strategy

Complete the parser adapter and its model first, then integrate the option and report. Preserve and verify the default path before adding the real-repository proof. The parser feature is complete only when valid FullMetalFile sources no longer appear as compiler syntax failures and the heuristic fallback remains visible and distinct.
