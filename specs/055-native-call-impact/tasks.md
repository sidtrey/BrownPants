# Tasks: Native Call Impact

**Input**: Design documents in `specs/055-native-call-impact/`

## Phase 1: Regression tests (User Story 1)

**Independent test**: Temporary mixed-language repositories show reverse native impact through qualified calls and MFC dispatch, preserve ambiguity/omissions as incomplete, and reject unsafe changed paths.

- [x] T001 [US1] Add failing analyzer and CLI regression tests for changed MFC handler and bridge target paths, candidate locations/status, ambiguity, unresolved edges, cycles, deduplication, unsafe paths, deterministic Markdown/JSON, and native evidence focus in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` and `tests/BrownPants.Tests/EvidenceCommandTests.cs`.

## Phase 2: Native impact analysis

- [x] T002 [US1] Add native impact finding and report evidence fields with explicit evidence kind, status, source/target locations, and completeness issues in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`.
- [x] T003 [US1] Implement reverse traversal across qualified-call and MFC-dispatch relations, with deterministic cycle guards, ambiguity handling, native relation omission handling, and bounded findings in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`.

## Phase 3: Mixed-language CLI and evidence handoff (User Story 2)

**Independent test**: A mixed C#/C++ request yields managed semantic relationships and native candidate relationships with separate evidence labels, while managed-only fixtures retain prior results.

- [x] T004 [US2] Extend changed-path validation and CLI help to accept discovered native sources alongside C#/VB while preserving rejection of traversal, missing files, and unsupported extensions in `src/BrownPants.Cli/Program.cs` and `src/BrownPants.Cli/EvidenceCommand.cs`.
- [x] T005 [US2] Integrate native changed-path selection and impact findings into engine-neutral evidence focus without relabeling lexical candidates as semantic calls in `src/BrownPants.Cli/Analysis/EvidenceFocusAnalyzer.cs` and `tests/BrownPants.Tests/EvidenceCommandTests.cs`.
- [x] T006 [US2] Render distinct managed/native impact sections, evidence classifications, source locations, and completeness notices in `src/BrownPants.Cli/Output/CallImpactReportRenderer.cs`; cover JSON/Markdown contract in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`.

## Phase 4: Brownfield verification and delivery

- [x] T007 [US1] Add a packed CLI smoke scenario for changed MFC view and bridge files, wire it into `.github/workflows/ci.yml`, and document native impact and evidence limits in `README.md` and `examples/brownfield-bug-lab/README.md`.
- [x] T008 [US1] Run BrownPants impact and evidence on the Brownfield Bug Lab, build/test the solution, run packed smoke and existing adapter checks, verify the acceptance hash and ledger, and record actual outputs in `specs/055-native-call-impact/verification.md`.

## Dependencies & Execution Order

- T001 establishes expected behavior before implementation.
- T002 defines report shape; T003 implements native traversal.
- T004/T005/T006 integrate path input, evidence selection, and presentation after the analyzer contract is stable.
- T007/T008 verify the packaged command and user-facing documentation.
- User Story 1 is the MVP; User Story 2 depends on the shared mixed-language report contract.

## Implementation Strategy

Deliver the native candidate trace first and validate it on the Brownfield Bug Lab. Then integrate mixed-language evidence handoff and run compatibility checks for managed-only impact. Finish with packaged CLI validation and report observed results without inferring runtime impact.

## Phase 5: Convergence

- [x] T009 [US1] Preserve original character offsets when removing trailing `//` comments from native source; add a regression with comments before the message map, handler definition, and qualified call, and verify exact reported source locations in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`, `src/BrownPants.Cli/Analysis/NativeFlowAnalyzer.cs`, and `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
