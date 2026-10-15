# Tasks: Managed Interop Impact

**Input**: Design documents in `specs/056-managed-interop-impact/`

## Phase 1: Regression fixtures (User Story 1)

**Independent test**: A temporary fixture traces a typed C++/CLI handle call to a unique C#/VB method and retains ambiguous, missing, and decoy cases without builds.

- [x] T001 [US1] Add red-first mixed C++/CLI/C#/VB fixture tests for typed receiver matching, unique target locations, overload/type ambiguity, unresolved methods, comments/strings, wrong receiver type, stable serialization, and no source writes in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

## Phase 2: Managed target index and bridge relations

- [x] T002 [US1] Add a bounded internal method declaration index to C# and VB syntax analysis without changing existing sampled report fields in `src/BrownPants.Cli/Analysis/CSharpAnalyzer.cs`, `VisualBasicAnalyzer.cs`, and `RepositoryInventory.cs`.
- [x] T003 [US1] Extract typed C++/CLI handle calls and match managed type/method declarations, retaining candidate target locations and explicit candidate/unresolved/ambiguous status in `src/BrownPants.Cli/Analysis/NativeFlowAnalyzer.cs`, `RepositoryInventory.cs`, and `ArchitecturePatternAnalyzer.cs`.
- [x] T004 [US2] Expose cross-language target locations and evidence limits in JSON/Markdown inventory and generic evidence output in `src/BrownPants.Cli/Analysis/EvidenceReport.cs`, `src/BrownPants.Cli/Output/MarkdownReport.cs`, and `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

## Phase 3: Managed-to-native impact trace (User Story 2)

**Independent test**: Selecting a C# or VB target path returns native bridge and upstream MFC candidate findings separately from managed compiler-resolved callers.

- [x] T005 [US2] Extend reverse native impact to seed from unique or possible managed target paths, retain ambiguous edges and candidate locations, and mark relevant truncation incomplete in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`, `EvidenceFocusAnalyzer.cs`, `CallImpactReportRenderer.cs`, and `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`.
- [x] T006 [US1] Replace the bridge's placeholder comment with an explicit C++/CLI call to `OrderImportWorkflow<T>.ImportBatch`, update the Brownfield Bug Lab walkthrough and README limits, and verify the fixture's actual source locations in `examples/brownfield-bug-lab/legacy/Bridge/OrderManagedBridge.cpp`, `examples/brownfield-bug-lab/README.md`, and `README.md`.

## Phase 4: Packaged validation and evidence

- [x] T007 [US1] Add packed CLI smoke for the complete MFC-to-C++/CLI-to-C# chain, assert evidence separation/locations and unchanged file hashes, and wire it into `.github/workflows/ci.yml` in `tests/PackedManagedInteropSmoke.ps1`.
- [x] T008 [US1] Run BrownPants inventory, evidence, and impact against BrownPants; build/test, install the packed tool and run smoke, run relevant Spec Kit/OpenSpec checks, verify hash/ledger, and record observed results in `specs/056-managed-interop-impact/verification.md`.

## Dependencies & Execution Order

- T001 fixes the acceptance answer key before implementation.
- T002 supplies the complete bounded managed syntax index; T003 consumes it to emit cross-language relations.
- T004 integrates inventory and SDD evidence; T005 enables managed-target impact through the native graph.
- T006 provides the end-to-end brownfield scenario; T007/T008 validate the distributable and record evidence.
- User Story 1 delivers the new bridge relation; User Story 2 adds impact and evidence selection.
