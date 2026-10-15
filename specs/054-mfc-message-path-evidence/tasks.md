# Tasks: MFC Message Path Evidence

**Input**: Design documents in `specs/054-mfc-message-path-evidence/`

## Phase 1: Regression fixtures (User Story 1)

**Independent test**: Temporary native repositories produce exact candidate paths from MFC command maps to handlers and qualified calls to target definitions; comments, literals, missing targets, and ambiguous targets never appear as successful edges.

- [x] T001 [US1] Add red-first temporary-repository tests for supported MFC command maps, cross-file handler/qualified-call definitions, missing and ambiguous targets, comment/string/raw-string decoys, stable JSON, and source read-only behavior in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

## Phase 2: Native flow indexing

- [x] T002 [US1] Implement bounded C/C++ source masking, MFC message-map extraction, class-qualified method definition indexing, handler matching, and qualified call matching with unresolved/ambiguous results in `src/BrownPants.Cli/Analysis/NativeFlowAnalyzer.cs`.
- [x] T003 [US1] Add native-flow relation/summary models and integrate indexing over discovered native sources in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.

## Phase 3: Report integration

- [x] T004 [US1] Add source/target location fields and candidate native-flow entries to the engine-neutral evidence report in `src/BrownPants.Cli/Analysis/EvidenceReport.cs`.
- [x] T005 [US1] Render native source-to-target candidates, unresolved/ambiguous counts, and lexical limits in `src/BrownPants.Cli/Output/MarkdownReport.cs`; verify JSON inventory and evidence commands in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

## Phase 4: Brownfield verification and documentation

- [x] T006 [US1] Add a packed CLI smoke check for the MFC-to-C++/CLI fixture, run it in `.github/workflows/ci.yml`, and document the candidate-only flow in `README.md`, `examples/brownfield-bug-lab/README.md`, and `specs/054-mfc-message-path-evidence/verification.md`.
- [x] T007 [US1] Run BrownPants inventory and evidence on the Brownfield Bug Lab; run release build/tests, pack smoke, acceptance hash, and evidence-ledger validation and record observed output in `specs/054-mfc-message-path-evidence/verification.md`.

## Dependencies & Execution Order

- T001 establishes the regression answer key before implementation.
- T002 and T003 build the analysis records and repository integration; T004 and T005 expose them.
- T006 documents and exercises the packaged CLI; T007 records end-to-end evidence.
- User Story 1 is the MVP and is independently verified by the temporary fixtures and Brownfield Bug Lab chain.

## Parallel Opportunities

- After T001 fixes expected report shapes, T002 and the documentation research for T006 can proceed independently. T003 depends on T002. Report work T004/T005 follows T003.
