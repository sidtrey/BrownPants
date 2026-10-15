# Tasks: GitHub Draft Pull Requests

**Input**: Design documents in `/specs/039-github-draft-pull-requests/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/github-pr.md`, and `quickstart.md`.

**Tests**: Required by AC-001–AC-006 and SC-001–SC-005. Use temporary repositories and injected HTTP handlers; do not make a live GitHub mutation.

## Phase 1: Provider-independent local preview

**Independent Test**: Build a preview from fixture feature artifacts without Azure configuration; assert zero HTTP requests, integrity metadata, evidence and gaps.

- [x] T001 [US1] Add temporary-repository tests for local preview creation without Azure DevOps configuration, network access, or credentials, covering intact/changed criteria and incomplete evidence in `tests/BrownPants.Tests/PullRequestPreviewCommandTests.cs`.
- [x] T002 [US1] Add `brownpants pr preview` argument handling and provider-independent offline execution that reuses `PullRequestPreviewAnalyzer` and `PullRequestPreviewRenderer` in `src/BrownPants.Cli/Analysis/PullRequestPreviewCommand.cs` and `src/BrownPants.Cli/Program.cs`; preserve repository-contained output safety and test in `PullRequestPreviewCommandTests.cs`.

## Phase 2: Explicit GitHub draft creation

**Independent Test**: Inject a recording HTTP handler and verify exactly one POST to GitHub.com with the intact preview's title/body/branches and `draft: true`; assert invalid or unconfirmed invocations make zero requests and no other mutation endpoint is called.

- [x] T003 [US2] Add mocked request-shape and failure tests for one draft create POST, redirects disabled, status/API errors, malformed/non-draft responses, transport ambiguity, token redaction, missing token, and zero-request gates in `tests/BrownPants.Tests/GitHubPullRequestTests.cs`.
- [x] T004 [US2] Implement fixed-host GitHub REST client and environment-only token handling in `src/BrownPants.Cli/Analysis/GitHubPullRequestClient.cs`; send only title/body/head/base/draft fields, pin the documented API version, reject redirects, validate returned draft/number/github.com URL, and never retry ambiguous failures.
- [x] T005 [US2] Add `brownpants pr create <repository-root> --provider github --preview <path> --repository <owner/name> --source <branch> --target <branch> --confirm --online` handling in `src/BrownPants.Cli/Analysis/GitHubPullRequestCommand.cs` and `src/BrownPants.Cli/Program.cs`; validate provider, preview integrity, explicit repository/branch consistency, flags, and root-contained preview path before reading credentials; test CLI gates and output in `GitHubPullRequestTests.cs`.

## Phase 3: Documentation and release verification

- [x] T006 [US1, US2] Document provider-independent previews and GitHub token permissions, environment handling, host limits, confirmation gates, and human-controlled follow-up in `integrations/github/README.md` and `README.md`; add a packed-tool fixture smoke check in `tests/PackedGitHubPullRequestSmoke.ps1` and invoke it from `.github/workflows/ci.yml`.
- [x] T007 [US1, US2] Run focused tests, Release build and full suite, acceptance hash/evidence validation, package audit, BrownPants-on-BrownPants inventory, and packed smoke; record actual outcomes in `specs/039-github-draft-pull-requests/verification.md`.

## Dependencies

- T001 precedes T002.
- T003 precedes T004–T005.
- T006 follows the CLI contract.
- T007 is the completion gate. Every task maps to acceptance/success criteria in `acceptance-evidence.json` before implementation.
