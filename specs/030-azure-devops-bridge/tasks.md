# Tasks: Azure DevOps Sprint and Delivery Traceability

**Input**: Design documents in `/specs/030-azure-devops-bridge/`
**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/azure-devops-integration.md`, and `quickstart.md`
**Tests**: Required by acceptance criteria. Add fixture-based HTTP contract and safety tests before implementation.

## Phase 1: Configuration and read-only traceability

**Goal**: A user can explicitly resolve feature work items and delivery links with bounded, reviewable evidence.

### Tests

- [X] T001 [US1] Add JSON configuration parsing, validation, token redaction, and path-containment cases in `tests/BrownPants.Tests/AzureDevOpsConfigurationTests.cs`.
- [X] T002 [US1] Add mocked HTTP contract tests for work-item fields, iteration metadata, PR/build/release relations, pagination, auth failures, throttling, partial responses, and zero requests without `--online` in `tests/BrownPants.Tests/AzureDevOpsClientTests.cs`.

### Implementation

- [X] T003 [US1] Add versioned feature mapping and connection/authentication configuration models in `src/BrownPants.Cli/Analysis/AzureDevOpsConfiguration.cs`.
- [X] T004 [US1] Implement a base-URL constrained, credential-redacting Azure DevOps REST client with injectable `HttpMessageHandler` and access-token provider in `src/BrownPants.Cli/Analysis/AzureDevOpsClient.cs`.
- [X] T005 [US1] Build deterministic work-item/iteration/delivery relation reports with completeness diagnostics in `src/BrownPants.Cli/Analysis/AzureDevOpsTraceabilityAnalyzer.cs`.
- [X] T006 [US1] Add versioned JSON and Markdown rendering in `src/BrownPants.Cli/Output/AzureDevOpsReportRenderer.cs`.
- [X] T007 [US1] Add `devops trace` argument parsing, explicit online opt-in, and safe error output in `src/BrownPants.Cli/Program.cs`.

## Phase 2: Reviewable PR preview

**Goal**: A contributor can inspect a preview that carries verified criteria/evidence and Azure DevOps links.

- [X] T008 [US2] Add preview contract tests for unchanged/changed criteria digest, task/evidence consistency, partial traceability, stable rendering, and output path safety in `tests/BrownPants.Tests/AzureDevOpsPullRequestPreviewTests.cs`.
- [X] T009 [US2] Build a local PR preview from the feature spec, criteria baseline, tasks, evidence ledger, and traceability report in `src/BrownPants.Cli/Analysis/PullRequestPreviewAnalyzer.cs` and `src/BrownPants.Cli/Output/PullRequestPreviewRenderer.cs`.
- [X] T010 [US2] Add `devops pr preview` CLI help and command handling in `src/BrownPants.Cli/Program.cs`.

## Phase 3: Explicit draft PR submission

**Goal**: A user can submit the reviewed preview as a draft PR without handing BrownPants review or merge authority.

- [X] T011 [US2] Add request-shape tests asserting one create endpoint, `isDraft: true`, and no reviewer/completion fields or other write endpoints in `tests/BrownPants.Tests/AzureDevOpsPullRequestSubmissionTests.cs`.
- [X] T012 [US2] Validate saved preview integrity and branch/repository inputs, then create and report one Azure Repos draft PR in `src/BrownPants.Cli/Analysis/AzureDevOpsPullRequestSubmitter.cs`.
- [X] T013 [US2] Add `devops pr create` confirmation flow and user-facing boundaries in `src/BrownPants.Cli/Program.cs`.

## Phase 4: Documentation and release verification

- [X] T014 [P] Document Azure DevOps Services/Server boundaries, Entra/PAT environment setup, least permission, privacy, and all command paths in `integrations/azure-devops/README.md` and `integrations/README.md`.
- [X] T015 [P] Link the integration and explain read-only traceability and explicit draft PR creation in `README.md`.
- [X] T016 [P] Add the offline mocked Azure DevOps contract suite to `.github/workflows/ci.yml`.
- [ ] T017 Run focused integration and xUnit tests, Release build, package smoke, acceptance hash/evidence checks, and cross-platform CI; record actual results in `specs/030-azure-devops-bridge/verification.md`.
- [X] T018 [US2] Add QA handoff tests and render each acceptance criterion with only documented test guidance, evidence, delivery links, and explicit gaps in the PR preview; update the integration quickstart and README.
- [X] T019 [US1] Add MSAL system-browser delegated auth for Azure DevOps Services, explicit tenant/client configuration, token-provider injection, cancellation handling, and a testable headless token mode in `src/BrownPants.Cli/Analysis/AzureDevOpsAuthentication.cs` and `Program.cs`.
- [X] T020 [US1] Add mocked auth-provider tests for online gating, cancellation, auth failure, token redaction, and no persistence in `tests/BrownPants.Tests/AzureDevOpsAuthenticationTests.cs`.

## Dependencies

- T001-T002 precede T003-T007.
- T004 feeds T005; T005 feeds T006 and T009.
- T008 precedes T009.
- T011 precedes T012-T013.
- T017 is the final gate. Every task must map to acceptance/success IDs in `acceptance-evidence.json` before implementation.
