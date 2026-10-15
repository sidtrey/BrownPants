# Feature Specification: GitHub Draft Pull Requests

**Feature Branch**: `039-github-draft-pull-requests`

**Created**: 2026-10-15

**Status**: Draft

**Input**: FIFO item 14: extend BrownPants' human-controlled, reviewable draft pull request workflow to another widely used Git provider. Start with GitHub.com, reuse the local acceptance and QA preview, and create only a draft pull request.

## User Scenarios & Testing

### User Story 1 - Prepare a provider-independent review preview (Priority: P1)

A contributor using GitHub wants BrownPants to prepare a local pull request draft from the feature's frozen acceptance criteria, task evidence, and documented QA guidance without requiring Azure DevOps configuration.

**Why this priority**: Teams need the same evidence-first handoff whether they use Azure Repos or GitHub.

**Independent Test**: Generate and inspect a local preview in a temporary repository with no Azure DevOps configuration or credentials.

**Acceptance Scenarios**:

1. **AC-001** Given a feature with acceptance criteria, tasks, and an evidence ledger, when a user prepares a local preview, then it contains the integrity-checked criteria digest, task/evidence status, QA guidance, and gaps, and does not require provider configuration or make a network request.
2. **AC-002** Given a missing or changed criteria baseline or incomplete evidence, when a preview is prepared, then the gaps remain visible and the preview is not labeled fully verified.

### User Story 2 - Create a GitHub draft PR with human control (Priority: P1)

A contributor wants to submit a reviewed BrownPants preview as a GitHub draft pull request while keeping review, approval, and merging under human control.

**Why this priority**: The user requested user-ready PR preparation across commonly used providers and explicitly prohibited self-approval or completion.

**Independent Test**: Use an injected HTTP handler to confirm the opt-in command issues exactly one GitHub draft-PR create request with the saved preview title, body, head, and base, and no other requests or writes.

**Acceptance Scenarios**:

1. **AC-003** Given an intact saved preview, explicit GitHub repository, and valid source/target branches, when a user runs the GitHub create command with both online opt-in and confirmation, then BrownPants sends one request for a draft PR containing the reviewed title and body and reports the returned PR number and URL.
2. **AC-004** Given no explicit confirmation or no online opt-in, an invalid/tampered preview, or missing credentials, when the create command runs, then it sends no HTTP request and reports the actionable problem without revealing credential values.
3. **AC-005** Given an ambiguous transport failure or response that does not confirm draft state, when creation fails, then BrownPants does not retry automatically and tells the user to inspect GitHub before trying again.
4. **AC-006** Given any create or failure path, when network operations are inspected, then the only permitted mutation is creating a draft PR; BrownPants never requests reviews, approves, merges, closes, edits a PR, modifies issues, or queues checks/releases.
5. **AC-007** Given a team preparing to use the GitHub adapter, when they follow its documentation, then they can identify the GitHub.com host boundary, required repository permission, environment-token handling, explicit creation gates, and human-controlled review/merge steps.

## Edge Cases

- The saved preview metadata or body digest is invalid or was edited after review.
- The branch or owner/repository identifier is malformed, equal source and target, or conflicts with the preview.
- GitHub returns rate limiting, permission errors, validation errors, malformed JSON, or an unexpected non-draft response.
- A timeout happens after GitHub may have created the PR; never retry automatically.
- The configured token lacks repository Pull requests write permission.
- The repository uses GitHub Enterprise Server; v1 does not support custom API hosts.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST prepare the same evidence-based local PR preview without requiring Azure DevOps configuration or making network requests.
- **FR-002**: BrownPants MUST require an intact preview, explicit GitHub owner/repository, source and target branches, confirmation, explicit online opt-in, and a fixed environment token before any GitHub request.
- **FR-003**: BrownPants MUST send only one GitHub.com draft pull request creation request and MUST verify the response identifies a draft PR before reporting success.
- **FR-004**: BrownPants MUST NOT approve, request reviewers, merge, close, update a pull request, modify issues, or create/queue builds, checks, or releases.
- **FR-005**: BrownPants MUST not persist credentials, print token values, automatically retry ambiguous create results, or follow response-provided links as request targets.
- **FR-006**: BrownPants MUST preserve existing Azure DevOps preview and draft-PR behavior.
- **FR-007**: Documentation MUST explain required GitHub repository permission, token handling, online/confirmation gates, supported host boundary, and the human-controlled follow-up.

### Key Entities

- **Saved PR preview**: A locally reviewed, integrity-protected title, body, repository and branch metadata, feature evidence, and content digest.
- **GitHub draft pull request**: A provider-side draft returned only after an explicit create operation and confirmed draft response.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The preview workflow passes with no Azure configuration and makes zero HTTP requests.
- **SC-002**: Every successful GitHub create call produces exactly one draft PR request and reports its provider-assigned number and URL.
- **SC-003**: Invalid, unconfirmed, or offline invocations produce zero HTTP requests.
- **SC-004**: Automated request-shape tests prove there are no review, approval, merge, close, issue mutation, build, or release operations.
- **SC-005**: Existing Azure DevOps PR tests remain passing, no new runtime NuGet package is required, and the integration guide covers each boundary in AC-007.

## Assumptions

- V1 supports GitHub.com only; GitHub Enterprise Server and other Git providers require separate compatibility work.
- Users provision a least-privilege GitHub credential through their approved environment process; BrownPants reads `BROWNPANTS_GITHUB_TOKEN` only after both explicit command gates.
- GitHub issue or project associations are not inferred from Azure work item IDs. The reviewed preview body remains the source for cross-system evidence links.
- Draft eligibility and token access are controlled by GitHub and repository policy.
