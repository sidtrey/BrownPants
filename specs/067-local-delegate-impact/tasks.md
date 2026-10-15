# Tasks: Local Delegate Impact

**Input**: Design documents in `specs/067-local-delegate-impact/`  
**Prerequisites**: Approved `spec.md`, verified `acceptance-criteria.sha256`, `plan.md`, research, data model, and delegate contract.

## Phase 1: Setup

**Purpose**: Map every task to frozen acceptance criteria before implementation.

- [x] T001 Create `specs/067-local-delegate-impact/acceptance-evidence.json` with a concrete verification command and criterion mapping for every task; initialize results as `planned`.

## Phase 2: Foundation

**Purpose**: No new project, package, or shared service is required. The semantic analyzer, impact graph, and existing Bug Lab are the shared foundations.

## Phase 3: User Story 1 — Trace a locally bound callback (P1)

**Independent test**: C# method-group and lambda bindings plus VB method-group bindings produce compiler-selected, source-located delegate paths without changing ordinary invocation totals.

- [x] T002 [US1] Add C# method-group, overloaded target, lambda, shorthand invocation, and explicit `Invoke` semantic fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`; assert target signatures and unchanged invocation counts.
- [x] T003 [US1] Add a VB method-group fixture in `tests/BrownPants.Tests/RepositoryScannerTests.cs` and verify the compiler-selected target and deterministic source location.
- [x] T004 [US1] Implement conservative single-assignment local delegate binding and emit `delegate` relationships in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`, preserving original method/constructor counts and canonical callable identities.
- [x] T005 [US1] Add impact tests in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` proving method-group edges reach the invocation caller and lambda-body calls continue through the lambda to its enclosing caller.
- [x] T006 [US1] Expose resolved delegate counts and relationship labels additively in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`, `src/BrownPants.Cli/Analysis/EvidenceReport.cs`, and `src/BrownPants.Cli/Output/MarkdownReport.cs`; cover stable JSON/Markdown and external target-path redaction in `tests/BrownPants.Tests/EvidenceCommandTests.cs` and `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

## Phase 4: User Story 2 — Make unsupported delegate flow visible (P2)

**Independent test**: Reassigned, captured, escaped, field/parameter-backed, and otherwise ambiguous delegate targets are not inferred; bounded samples appear and managed impact becomes incomplete.

- [x] T007 [P] [US2] Add reassignment, capture, escape, parameter, and field delegate fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`; assert there is no false resolved delegate edge.
- [x] T008 [US2] Track exact unresolved delegate totals and bounded source-located samples in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs` with deterministic reason ordering.
- [x] T009 [US2] Make unresolved delegate targets mark managed impact incomplete in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`; render counts, samples, and limitations in `src/BrownPants.Cli/Output/MarkdownReport.cs` and `src/BrownPants.Cli/Analysis/EvidenceReport.cs`, with tests in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` and `tests/BrownPants.Tests/EvidenceCommandTests.cs`.

## Phase 5: User Story 3 — Prove the value in a matched task (P1)

**Independent test**: Three fresh, source-identical baseline/assisted Codex pairs use the frozen task, model, CLI, sandbox, and schema; the assisted condition meets the approved managed-impact recall/precision threshold and all six fixes pass the same independent check.

- [x] T010 [US3] Create the standalone delegate persistence fixture, freeze the task, prompt, schema, and source-derived answer key with separate SHA-256 files, and verify frozen inputs before agent runs.
- [x] T011 [US3] Prepare three matched pairs using `Prepare-DelegateComparison.ps1`; verify clean and seeded checks, identical code digests, and that only assisted copies receive BrownPants evidence while scorer/key files remain outside the workspaces.
- [x] T012 [US3] Run three counterbalanced pairs with preserved JSONL events, structured responses, usage, and post-fix logs; self-test the scorer on exact matches, duplicates, omissions, and decoys.
- [x] T013 [US3] Execute all six Codex runs in isolated copies; preserve source/task/key/model/CLI/prompt/schema/sandbox digests, raw results, token usage, and post-fix source/checks under `evaluation/delegate-impact/runs/`; publish the scored report and JSON under `docs/comparisons/`.
- [x] T014 [US3] Update `README.md` and comparison docs from the scored results; scope the advantage claim to this fixture and report recall, precision, token usage, pair outcomes, and limitations.

## Phase 6: Polish and release verification

- [x] T015 Verify the unchanged acceptance hash; run focused tests, the full Release build/test suite, the Bug Lab check `dotnet run --project examples/brownfield-bug-lab/managed/tests/LegacyOrderDesk.Checks.csproj -c Release`, Bug Lab impact, and semantic self-inspection; validate the evidence ledger and record observed output in `specs/067-local-delegate-impact/verification.md`. The PR must carry the maintainer-approved `acceptance-criteria-reviewed` label before merge; this external review gate is recorded in `verification.md`.

## Dependencies and Execution Order

- T001 precedes all implementation.
- T002–T003 establish failing semantic regressions before T004; T005–T006 follow the graph implementation.
- T007 precedes T008; T009 follows unresolved-flow implementation.
- T010 freezes the task before T011–T013. T013 follows implemented analysis and working preparation/runner/scorer scripts. T014 follows scored output.
- T015 follows all implementation and benchmark work.
- User stories are sequential because they extend shared Roslyn semantic records and rely on User Story 1's graph.

## Parallel Opportunities

- T002 and T003 target the same test file, so they are sequential despite independent language fixtures.
- Once T004 is complete, T005 and T007 can be authored in separate test files, but execute the shared suite serially.
- No user story is independently shippable before the shared graph is complete; the matched benchmark depends on T004–T009.

## Implementation Strategy

Deliver safe resolved edges first, then unsupported-flow visibility. Freeze the benchmark inputs before running any agents. Treat a failed comparative threshold as a release-goal failure: preserve the honest result and improve BrownPants or the user workflow before claiming an advantage. Finish with the complete release verification in T015.
