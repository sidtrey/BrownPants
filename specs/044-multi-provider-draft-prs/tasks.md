# Tasks: Multi-Provider Draft Pull Requests

**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Phase 1: Shared Submission Contract

- [x] T001 [US3] Review shared preview and provider dispatch contracts in `src/BrownPants.Cli/Analysis/PullRequestPreviewRenderer.cs` and `src/BrownPants.Cli/Program.cs`; preserve local-only preview behavior.
- [x] T002 [US3] Add regression coverage for preview/provider compatibility in `tests/BrownPants.Tests/PullRequestPreviewCommandTests.cs`.

## Phase 2: GitLab.com Draft Merge Requests (US1)

- [x] T003 [US1] Implement fixed-host GitLab API client with draft-title creation and response validation in `src/BrownPants.Cli/Analysis/GitLabMergeRequestClient.cs`.
- [x] T004 [US1] Implement gated `gitlab` provider command with repository-relative intact preview checks and credential loading in `src/BrownPants.Cli/Analysis/GitLabMergeRequestCommand.cs` and `src/BrownPants.Cli/Program.cs`.
- [x] T005 [US1] Add success, request shape, gate, redaction, tamper, response, redirect, and timeout tests in `tests/BrownPants.Tests/GitLabMergeRequestTests.cs`.
- [x] T006 [US1] Document GitLab.com host, token handling, permissions, and human follow-up in `integrations/gitlab/README.md`.

## Phase 3: Bitbucket Cloud Draft Pull Requests (US2)

- [x] T007 [US2] Implement fixed-host Bitbucket API client with `draft: true` and response validation in `src/BrownPants.Cli/Analysis/BitbucketPullRequestClient.cs`.
- [x] T008 [US2] Implement gated `bitbucket` provider command and CLI dispatch in `src/BrownPants.Cli/Analysis/BitbucketPullRequestCommand.cs` and `src/BrownPants.Cli/Program.cs`.
- [x] T009 [US2] Add success, request shape, gate, redaction, tamper, response, redirect, and timeout tests in `tests/BrownPants.Tests/BitbucketPullRequestTests.cs`.
- [x] T010 [US2] Document Bitbucket Cloud host, token handling, permissions, and human follow-up in `integrations/bitbucket/README.md`.

## Phase 4: Compatibility and Release Evidence

- [x] T011 [US3] Update README provider matrix and `IDEA-QUEUE.md` item 14 with verified support boundaries.
- [x] T012 [US3] Verify frozen criteria and run full build, tests, package audit, CLI help and offline preview smoke; record outputs under `specs/044-multi-provider-draft-prs/evidence/`.
- [x] T013 [US3] Map completed tasks to acceptance IDs and evidence in `acceptance-evidence.json`; validate hash and ledger.

