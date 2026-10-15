# Tasks: Task-Based Model Guidance

**Input**: Design documents from `/specs/038-model-routing-policy/`

## Phase 1: Policy parsing and reports

**Independent Test**: Load valid and invalid policy fixtures from temporary repositories and compare deterministic Markdown/JSON output.

- [x] T001 [US1] Implement bounded versioned model-policy parsing, validation, category/profile checks, and repository-contained path resolution in `src/BrownPants.Cli/Analysis/ModelRoutingPolicy.cs`; add unit tests in `tests/BrownPants.Tests/ModelRoutingPolicyTests.cs`.
- [x] T002 [US1] Implement stable Markdown/JSON policy and host capability reports in `src/BrownPants.Cli/Analysis/ModelRoutingReportRenderer.cs`; add output equivalence and ordering tests in `tests/BrownPants.Tests/ModelRoutingCommandTests.cs`.

## Phase 2: Explicit host profile export

**Independent Test**: Export a valid profile set into temporary project roots for each host, repeat identical exports, and verify conflicts, path escapes, and invalid inputs never result in partial or external writes.

- [x] T003 [US2] Implement host-specific profile serialization for VS Code/Copilot `.agent.md`, Visual Studio 18.4+ `.agent.md` with the distinct `brownpants-visual-studio-<id>` filename/display name under the shared `.github/agents/` directory, Claude Code `.md`, and Codex `.toml` with deterministic model-field escaping in `src/BrownPants.Cli/Analysis/ModelRoutingProfileExporter.cs`; add per-host serialization and filename-isolation tests in `tests/BrownPants.Tests/ModelRoutingProfileExporterTests.cs`.
- [x] T004 [US2, US3] Implement explicit-host export preflight, idempotency, conflict preservation, link/reparse-point containment, and no-partial-write behavior in `ModelRoutingProfileExporter.cs`; test through isolated temporary repositories in `tests/BrownPants.Tests/ModelRoutingProfileExporterTests.cs`.

## Phase 3: CLI, documentation, and example

**Independent Test**: Exercise `models validate`, `models report`, and `models export` through `Program.Run` using temporary roots; validate the documented sample policy.

- [x] T005 [US1, US3] Add `brownpants models validate|report|export` argument parsing, exit codes, repository/policy options, and Markdown/JSON selection to `src/BrownPants.Cli/Program.cs`; expose `--host visual-studio` in CLI help and supported-host errors, and cover usage and errors in `tests/BrownPants.Tests/ModelRoutingCommandTests.cs`.
- [x] T006 [US1, US2, US3] Document policy setup, all four host formats and limitations, Visual Studio's 18.4+ requirement/shared-directory naming, model availability caveats, and no-provider-call behavior in `docs/model-routing.md`; validate the four-host example at `examples/model-routing-policy.json`, link it from `README.md`, and extend the packed-tool smoke check in `.github/workflows/ci.yml` to validate and export Visual Studio profiles.

## Phase 4: Verification and evidence

- [x] T007 [US1, US2, US3] Verify all acceptance and success criteria with focused model-policy tests, `dotnet build BrownPants.sln -c Release --no-incremental`, `dotnet test BrownPants.sln -c Release --no-build`, acceptance hash/evidence checks, package audit, and BrownPants-on-BrownPants inventory; record observed results in `specs/038-model-routing-policy/verification.md`.

## Dependencies

T001 precedes T002–T005. T003 precedes T004. T006 follows the finalized command contract. T007 is the completion gate.
