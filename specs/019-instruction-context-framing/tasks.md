# Tasks: Instruction Context Frames

**Input**: Design documents in `specs/019-instruction-context-framing/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, and `contracts/instruction-context-frame.schema.json`

**Acceptance criteria integrity**: This feature's acceptance scenarios and success criteria are frozen by [acceptance-criteria.sha256](acceptance-criteria.sha256). Verify before implementation and completion with `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/019-instruction-context-framing/spec.md`. If the check fails, review the change with the user; refresh the digest only after explicit approval.

## Phase 1: Shared Foundations

- [X] T001 Define instruction source and response frame report records in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs`.
- [X] T002 Add deterministic Markdown heading-anchor extraction, byte/token estimates, SHA-256 identity, and bounded reads in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs`.
- [X] T003 [P] Add fixture helpers for mixed instruction conventions and scoped rules in `tests/BrownPants.Tests/InstructionSourceAnalyzerTests.cs`.

## Phase 2: User Story 1 - Map the Instruction Set (P1)

**Independent test**: A mixed temporary repository produces stable source paths, scope evidence, anchors, sizes, and limitations in Markdown and JSON.

- [X] T004 [US1] Discover common global and scoped instruction conventions and explicitly selected in-repository files in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs`.
- [X] T005 [US1] Expose a schema-versioned instruction-source report through the standalone `instructions` command in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs` and `src/BrownPants.Cli/Program.cs`.
- [X] T006 [US1] Render recognized conventions, scope evidence, headings, approximate token cost, and unsupported/read issues in `src/BrownPants.Cli/Output/InstructionSourceReportRenderer.cs`.
- [X] T007 [US1] Add `instructions` command with `--source`, `--format`, and bounded-read behavior and update help in `src/BrownPants.Cli/Program.cs`.
- [X] T008 [P] [US1] Test nested scope, custom sources, path traversal rejection, stable ordering, and JSON behavior in `tests/BrownPants.Tests/InstructionSourceAnalyzerTests.cs`.

## Phase 3: User Story 2 - Create and Validate a Response Frame (P1)

**Independent test**: A frame with section citations validates against the source inventory, detects changed sources, reports unresolved review items and budget overflow, and never edits source files.

- [X] T009 [US2] Define the JSON draft input, output records, validation errors, word/token budget counting, and source-reference validation in `src/BrownPants.Cli/Analysis/InstructionContextFrameAnalyzer.cs` and `specs/019-instruction-context-framing/contracts/instruction-context-frame.schema.json`.
- [X] T010 [US2] Add explicit `frame validate` and `frame create` output paths, overwrite protection, and repository boundary checks in `src/BrownPants.Cli/Program.cs`.
- [X] T011 [US2] Render validated frame points with exact Markdown links, digests, stale-source warnings, conflicts/review items, and budget caveats in `src/BrownPants.Cli/Analysis/InstructionContextFrameAnalyzer.cs`.
- [X] T012 [P] [US2] Test valid and invalid citations, digest changes, word/token budgets, overwrite protection, path traversal, and source preservation in `tests/BrownPants.Tests/InstructionSourceAnalyzerTests.cs`.

## Phase 4: User Story 3 - Apply a Frame in Spec Kit (P2)

**Independent test**: Spec Kit guidance consumes a validated frame first, reads cited sections when relevant, and states host-loading limitations.

- [X] T013 [US3] Extend the Spec Kit context guidance under `integrations/speckit/commands/context.md` with frame drafting format, retrieval policy, conflict review, and host caveat.
- [X] T014 [US3] Synchronize generated guidance in `.specify/extensions/brownpants/commands/context.md` and `.agents/skills/speckit-brownpants-context/SKILL.md`; verify parity with `tests/SpeckitExtensionTests.ps1`.
- [X] T015 [P] [US3] Add integration test for frame guidance and lifecycle behavior in `tests/SpeckitExtensionTests.ps1`.

## Phase 5: Documentation and Verification

- [X] T016 Document instruction discovery, frame creation/validation, options, supported conventions, and limitations in `README.md` and `specs/019-instruction-context-framing/quickstart.md`.
- [X] T017 Add reusable acceptance-criteria hashing and drift verification in `scripts/acceptance-criteria-hash.ps1`, initialize this feature's baseline, and add regression coverage in `tests/AcceptanceCriteriaHashTests.ps1`.
- [X] T018 Wire acceptance hash regression and active-feature baseline checks into `.github/workflows/ci.yml`, enforce the workflow in Spec Kit context guidance, and document intentional re-baselining.
- [X] T019 Run acceptance hash verification, `dotnet build BrownPants.sln -c Release`, `dotnet test BrownPants.sln -c Release`, both adapter checks, packed CLI smoke checks, and `git diff --check`; fix regressions.

## Dependencies

- T001-T003 establish shared types and tests.
- User Story 1 tasks depend on T001-T003; T007 uses the analyzer from T004-T006.
- User Story 2 depends on the source map and anchors from User Story 1.
- User Story 3 depends on the stable frame format and CLI validation from User Story 2.
- Final docs and verification depend on all implementation tasks.
