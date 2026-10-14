# Tasks: Requirement Placement Guidance

**Input**: Design documents in `specs/020-requirement-placement-guidance/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, and `contracts/placement-guidance.schema.json`

**Acceptance criteria integrity**: The scenarios and success criteria are frozen in [acceptance-criteria.sha256](acceptance-criteria.sha256). Verify before implementation and completion with `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/020-requirement-placement-guidance/spec.md`. A mismatch requires review; refresh only after explicit agreement.

## Phase 1: User Story 1 - Recommend a Placement (P1)

**Independent test**: Canonical principle, feature outcome, and agent procedure inputs receive the matching destination with rationale tied to the supplied attributes.

- [X] T001 [US1] Add decision table tests for repository/enduring/principle, feature/outcome, and agent workflow cases in `tests/BrownPants.Tests/PlacementGuidanceTests.cs`.
- [X] T002 [US1] Define candidate input and versioned report records in `src/BrownPants.Cli/Analysis/PlacementGuidanceAnalyzer.cs`.
- [X] T003 [US1] Implement deterministic purpose/scope/durability mapping and evidence-based rationale in `src/BrownPants.Cli/Analysis/PlacementGuidanceAnalyzer.cs`.
- [X] T004 [US1] Add `placement` CLI parsing for `--rule`, `--scope`, `--durability`, `--purpose`, and `--format` in `src/BrownPants.Cli/Program.cs`.

## Phase 2: User Story 2 - Surface Ambiguity and Mixed Rules (P1)

**Independent test**: Unknown, incompatible, and mixed inputs produce questions or split destinations instead of unsupported certainty.

- [X] T005 [US2] Test unknown fields, conflicting dimensions, mixed purpose, and repository-wide outcome split in `tests/BrownPants.Tests/PlacementGuidanceTests.cs`.
- [X] T006 [US2] Implement clarification question generation and split recommendations in `src/BrownPants.Cli/Analysis/PlacementGuidanceAnalyzer.cs`.
- [X] T007 [US2] Validate enum values and reject empty/oversized rule text with clear CLI errors in `src/BrownPants.Cli/Program.cs`.

## Phase 3: User Story 3 - Use Portable, Reviewable Output (P2)

**Independent test**: Markdown and JSON carry equivalent fields; identical inputs produce byte-identical output and do not modify repository files.

- [X] T008 [US3] Test schema-versioned JSON, deterministic Markdown/JSON, and rationale/questions parity in `tests/BrownPants.Tests/PlacementGuidanceTests.cs`; review that the CLI decision path performs no file or network operations.
- [X] T009 [US3] Render recommendations, destinations, rationale, questions, and evidence limits in `src/BrownPants.Cli/Output/PlacementGuidanceRenderer.cs`.
- [X] T010 [US3] Verify output contract fields against `specs/020-requirement-placement-guidance/contracts/placement-guidance.schema.json` in CLI tests.
- [X] T011 [US3] Document the placement rubric and command examples in `README.md` and `specs/020-requirement-placement-guidance/quickstart.md`.
- [X] T012 [US3] Add Spec Kit context guidance for explicit classification inputs and synchronize the installed context skill in `integrations/speckit/commands/context.md`, `.specify/extensions/brownpants/commands/context.md`, and `.agents/skills/speckit-brownpants-context/SKILL.md`; assert it in `tests/SpeckitExtensionTests.ps1`.

## Phase 4: Verification

- [X] T013 Verify `acceptance-criteria.sha256`, Release build, xUnit tests, Spec Kit/OpenSpec adapter checks, packed CLI smoke cases, and `git diff --check`; fix failures.

## Dependencies

- T001 precedes T002-T004; tests should fail before the rule implementation is added.
- US2 tests and behavior build on US1's report and rule model.
- US3 rendering and contract tests build on both prior stories.
- Run T013 after all implementation and documentation tasks.
