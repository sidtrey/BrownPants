# Feature Specification: Azure DevOps Sprint and Delivery Traceability

**Feature Branch**: `030-azure-devops-bridge`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Idea queue item 8: bridge spec-driven development with Agile and Azure DevOps workflows, reference related delivery evidence for QA, and prepare a reviewable mock PR.

## User Scenarios & Testing

### User Story 1 - Connect a feature to sprint work and delivery evidence (Priority: P1)

A developer or QA reviewer wants the active feature's story and mapped child tasks, sprint/iteration state, and related pull requests, builds, and releases gathered in one report so they can follow each role's work from planning through deployment. A developer may own code, test, documentation, and PR tasks; QA needs the related acceptance criteria, verification guidance, and delivery evidence after the PR passes.

**Why this priority**: Teams currently hand-copy links among specs, work items, builds, PRs, and releases. A traceable report can remove that work while leaving Azure DevOps as the planning authority.

**Independent Test**: Use a temporary repository, fixture configuration, and mocked Azure DevOps REST responses to confirm feature-to-work-item mapping, iteration fields, related artifact links, deterministic output, and explicit gaps.

**Acceptance Scenarios**:

1. **AC-001** — Given a feature with explicit Azure DevOps work-item mappings and a configured connection, when a user explicitly requests an online traceability report, then it lists the work-item title, type, state, iteration path, and source URL for each mapped item.
2. **AC-002** — Given mapped work items with related pull request, build, or release references available through Azure DevOps, when the report is generated, then it includes their titles or identifiers, statuses when available, and clickable source URLs, grouped by artifact type.
3. **AC-003** — Given a missing mapping, inaccessible item, unsupported artifact relationship, or partial API failure, when the report is generated, then it names the missing evidence and marks the report incomplete instead of implying that no related work exists.
4. **AC-004** — Given unchanged repository files and equivalent service responses, when Markdown or JSON output is generated repeatedly, then findings and ordering are deterministic and the two formats contain equivalent information.

### User Story 2 - Prepare and optionally submit a draft PR (Priority: P1)

A developer wants a draft PR summary assembled from a feature's fixed acceptance criteria, task status, verification evidence, and Azure DevOps links. QA also needs a knowledge-transfer handoff focused on how to test each acceptance criterion and what evidence exists. After reviewing the PR preview and QA handoff, the developer may ask BrownPants to create a draft PR in Azure Repos Git or submit it themselves through their normal workflow.

**Why this priority**: QA and reviewers need one reliable handoff with the relevant planning and delivery links, while the actual PR remains a human-controlled step.

**Independent Test**: Generate a local PR preview from a fixture feature and traceability response, inspect its criteria digest, task/evidence table, and links, then use a mocked Azure DevOps client to confirm explicit submission creates only a draft PR.

**Acceptance Scenarios**:

1. **AC-005** — Given a feature with an unchanged acceptance-criteria baseline and an evidence ledger, when a user prepares a local PR preview, then it includes the feature summary, criteria digest, task completion/evidence status, and known work-item/build/PR/release links.
2. **AC-006** — Given a missing or changed criteria baseline, incomplete task evidence, or partial Azure DevOps data, when a user prepares a local PR preview, then the draft labels those gaps and cannot describe the feature as fully verified.
3. **AC-007** — Given a reviewed local PR preview, when a user invokes the explicit create-draft-PR command with confirmation, source/target branches, and mapped work items, then BrownPants creates one Azure Repos Git pull request in draft state, associates the mapped work items, and reports its returned URL and identifier.
4. **AC-008** — Given any traceability or PR command, when it runs, then online access requires explicit opt-in; BrownPants never approves, votes on, auto-completes, merges, completes, updates, or abandons a PR, never changes work-item fields or workflow state, and never queues builds or creates releases. Draft PR creation may associate mapped work items in that single create request.
5. **AC-009** — Given a feature with acceptance criteria and available verification evidence, when a user prepares a PR preview, then it includes a QA knowledge-transfer section with each criterion, actionable test guidance derived from feature documentation and evidence, and known PR/build/release links; unsupported test steps remain explicit gaps rather than invented instructions.
6. **AC-010** — Given an Azure DevOps Services operation with `--online` and browser authentication selected, when the user signs in, then BrownPants obtains a delegated Microsoft Entra token through the system browser and acts only with that user's Azure DevOps permissions. BrownPants never writes the token to repository files, reports, or logs; authentication cancellation or tenant-policy denial stops the operation without making Azure DevOps requests.

## Edge Cases

- Azure DevOps is not configured, the user does not request online access, or no credential is available.
- The configured environment variable is unset, rejected, expired, or lacks read permission.
- Interactive Entra sign-in is unavailable, blocked by tenant policy, or lacks configured client/tenant IDs; a headless caller selects token authentication.
- A mapped work item was deleted, moved between projects, or is hidden by permissions.
- An iteration has no dates or no longer corresponds to the current sprint.
- Work-item relations omit builds, PRs, or release history; builds may be associated only with a subset of work items.
- Azure DevOps paginates, throttles, returns an unsupported API response, or is temporarily unreachable.
- A feature has no acceptance baseline, ledger, or evidence paths, or its task state disagrees with the evidence ledger.
- Output path conflicts with existing content or resolves outside the selected repository.
- A report or mock PR could expose organization names, work-item titles, or repository paths when shared externally.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST support an explicit feature-to-Azure-DevOps work-item mapping that keeps the team's work-item system authoritative.
- **FR-002**: The system MUST provide a user-invoked traceability report for mapped work items, sprint/iteration metadata, and related builds, pull requests, and releases when the configured service exposes those relationships.
- **FR-003**: The system MUST identify which artifacts were directly returned by Azure DevOps and which relationships were unavailable or inferred from configured references.
- **FR-004**: The system MUST offer deterministic Markdown and versioned JSON reports with repository-relative feature/evidence paths and clickable Azure DevOps source links.
- **FR-005**: The system MUST prepare a local, reviewable PR preview from the selected feature's specification, acceptance-criteria baseline, task list, evidence ledger, and available traceability report.
- **FR-006**: The system MUST identify missing, stale, conflicting, or incomplete acceptance and verification evidence in the mock PR draft; it MUST NOT label such work fully verified.
- **FR-007**: Azure DevOps access MUST require explicit online opt-in. Azure DevOps Services MUST support delegated Microsoft Entra browser authentication using an explicitly configured public-client ID and tenant; a headless/environment-token mode MUST remain available through the fixed `BROWNPANTS_AZURE_DEVOPS_TOKEN` variable. Tokens MUST NOT be printed or persisted by BrownPants and MUST use only the permissions required by the requested operation. Traceability reads MUST work with read-only access; draft PR creation requires the provider's PR contribution permission. Azure DevOps Server MUST use a supported PAT or Windows-authentication path, not Entra browser authentication.
- **FR-008**: The integration MUST use documented, versioned Azure DevOps APIs and report authorization, pagination, throttling, unsupported-data, and service errors as bounded diagnostics.
- **FR-009**: After an explicit create-draft-PR command with confirmation, the integration MAY create a single Azure Repos Git PR with `isDraft` set and associate the mapped work items in that create request. It MUST NOT approve/vote, enable auto-completion, merge, complete, update, or abandon a PR, change work-item fields or workflow state, queue builds, or create releases.
- **FR-010**: The PR creation request MUST use only the reviewed preview's title, description, and mapped work-item references plus explicitly supplied repository, source branch, and target branch. It MUST omit reviewer votes and all completion options. The description MUST avoid Azure Boards transition keywords that could change work-item state.
- **FR-011**: The default inspect and other offline commands MUST NOT access Azure DevOps or require integration credentials.
- **FR-012**: Documentation MUST explain setup, credential handling, permissions, supported relationship coverage, and report privacy limits.
- **FR-013**: The PR preview MUST provide a QA handoff that links acceptance criteria to existing verification instructions and evidence. It MAY organize explicitly mapped story and task work items by relations returned from Azure DevOps, but MUST NOT invent task completion, test procedures, or delivery links.
- **FR-014**: Interactive authentication MUST use MSAL user-delegated sign-in in the system browser, honor MFA and Conditional Access, and request only the Azure DevOps resource audience needed by this integration. Cancellation, denied consent, or blocked sign-in MUST fail closed with a sanitized explanation. Token contents MUST remain opaque.

### Key Entities

- **Feature mapping**: A repository-local association from a Spec Kit feature path to one or more Azure DevOps work-item identifiers.
- **Traceability report**: A deterministic snapshot of observed work-item, iteration, and related delivery references with source URLs and completeness diagnostics.
- **PR preview**: A local review artifact summarizing a feature, its frozen criteria, task evidence, and related Azure DevOps links. A user may submit this preview as a draft PR through a separate explicit command.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A configured feature with mapped work items produces a report containing each returned item's title, state, iteration path, and source URL.
- **SC-002**: Fixture responses containing related PRs, builds, and releases yield correctly typed and clickable references; missing relationships are marked unknown rather than absent.
- **SC-003**: Repeated fixture runs produce byte-equivalent normalized JSON and equivalent ordered Markdown findings.
- **SC-004**: A complete feature produces a mock PR draft that includes its unchanged criteria digest and links every completed task to existing verification evidence.
- **SC-005**: Incomplete or changed criteria/evidence produces a clearly incomplete preview; tests confirm the explicit create path sends only a draft-PR creation request and no approval or completion operation.
- **SC-006**: With no online opt-in, commands perform zero HTTP requests; no secret value appears in logs, reports, or generated files.
- **SC-007**: A QA reviewer can use the PR preview to find every acceptance criterion, its documented test guidance or visible guidance gap, the available verification evidence, and known post-PR build/release links without asking the developer to manually restate those details.
- **SC-008**: Mocked authentication-flow tests verify browser sign-in is invoked only after `--online`, cancellation makes zero Azure DevOps requests, and the acquired token is never written to reports, config, or logs.

## Assumptions

- The first provider is Azure DevOps Services and its versioned REST API; other Agile platforms remain separate adapters.
- Users or administrators provision a short-lived Microsoft Entra bearer token when available. Traceability uses read permissions; creating a draft PR needs Azure Repos PR contribution permission. A short-lived PAT may be supplied for supported legacy or Azure DevOps Server environments. Tokens are passed via the fixed environment variable and never stored in repo config.
- Work-item mappings are explicit and reviewable in the repository, while credentials and user-specific values remain external.
- Creating a remote PR requires an explicit user-invoked command after preview; BrownPants never self-approves or completes that PR.
- Azure DevOps relationships are incomplete when organization permissions, custom process rules, retention, or API coverage hide associated artifacts; BrownPants reports those limits.
