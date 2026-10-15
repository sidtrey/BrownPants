# Feature Specification: Multi-Provider Draft Pull Requests

**Feature Branch**: `044-multi-provider-draft-prs`
**Created**: 2026-10-15
**Status**: Completed
**Input**: Extend BrownPants' local evidence-backed draft pull request workflow to GitLab.com and Bitbucket Cloud, preserving explicit user confirmation and human-controlled review.

## User Scenarios & Testing

### User Story 1 - Create a GitLab.com draft merge request (Priority: P1)
A contributor prepares a shared local evidence preview, reviews it, and explicitly creates one GitLab.com draft merge request.

**Independent Test**: Inject an HTTP handler and verify the fixed GitLab API request, draft title, credential header, and confirmed response without contacting GitLab.

**Acceptance Scenarios**:
1. **AC-001** Given a valid unmodified saved preview and explicit project/branches, when the user opts into online creation and confirms, then one GitLab.com create request is sent with a `Draft:` title and the reviewed description, and the returned merge request URL is reported.
2. **AC-002** Given missing confirmation, online opt-in, token, valid preview, or valid project/branch metadata, when creation is attempted, then no request is sent and credential values are never printed.
3. **AC-003** Given an ambiguous failure or response that does not confirm an open draft merge request, when creation fails, then BrownPants does not retry and instructs the user to inspect GitLab before retrying.

### User Story 2 - Create a Bitbucket Cloud draft pull request (Priority: P1)
A contributor creates one draft pull request from the same locally reviewed preview in Bitbucket Cloud.

**Independent Test**: Inject an HTTP handler and verify the fixed Bitbucket Cloud API request includes draft state, reviewed content, and no follow-up mutations.

**Acceptance Scenarios**:
1. **AC-004** Given a valid saved preview and explicit workspace/repository/branches, when the user opts into online creation and confirms, then one Bitbucket Cloud create request is sent with `draft: true`, and the returned PR URL is reported.
2. **AC-005** Given missing gates, invalid or edited preview, metadata mismatch, or missing credential, when creation is attempted, then no request is sent and credential values are never printed.
3. **AC-006** Given an ambiguous failure or response that does not confirm an open draft PR, when creation fails, then BrownPants does not retry and instructs the user to inspect Bitbucket before retrying.

### User Story 3 - Preserve provider-independent preparation (Priority: P1)
Teams prepare the evidence preview offline regardless of provider and retain existing GitHub and Azure Repos behavior.

**Independent Test**: Existing preview tests and provider submission tests pass without credentials or network access.

**Acceptance Scenarios**:
1. **AC-007** Given no provider configuration or credentials, when a user prepares a preview, then it is generated locally with criteria digest, task/evidence state, QA guidance, and gaps.
2. **AC-008** Given any create path, when requests are inspected, then the only permitted provider mutation is creating one draft; approval, review requests, merge, close, edits, issues, builds, and releases are never requested.

## Requirements

- **FR-001**: Both adapters MUST require a saved intact preview, explicit provider repository and branch metadata matching the preview, explicit `--online` and `--confirm`, and provider-specific environment credentials before making a request.
- **FR-002**: GitLab MUST use only the fixed GitLab.com API host and create a draft using a `Draft:` title; self-managed instances are outside this feature.
- **FR-003**: Bitbucket MUST use only the fixed Bitbucket Cloud API host and create a draft using the API's draft field; Data Center is outside this feature.
- **FR-004**: Each adapter MUST send at most one create request, verify provider response state and a valid HTTPS provider URL, and never retry ambiguous outcomes automatically.
- **FR-005**: Credentials MUST not be persisted, included in diagnostics, or used before explicit gates pass. Response-provided URLs MUST never become request targets.
- **FR-006**: No additional runtime NuGet dependency is allowed; Azure Repos and GitHub behavior MUST remain compatible.
- **FR-007**: Documentation MUST explain host boundaries, least privilege, credential variable, confirmation gates, draft semantics, and human review/merge follow-up.

## Success Criteria

- **SC-001**: Automated request-shape tests prove each adapter sends exactly one create-draft request on success and zero requests on rejected input.
- **SC-002**: Tests prove no automatic retry, token leakage, or non-create mutation for either provider.
- **SC-003**: Existing Azure Repos, GitHub, local preview, full build, and test suites pass; no new runtime package is added.
- **SC-004**: A user can follow provider documentation to create a draft without guessing endpoint, scopes/permissions, or confirmation steps.

## Assumptions

- Provider endpoints are GitLab.com REST API v4 and Bitbucket Cloud REST API 2.0 only.
- Teams create or review local previews before provider submission; this work does not publish, review, approve, merge, or close changes.
- Provider API permissions and account policy determine whether draft creation succeeds.
