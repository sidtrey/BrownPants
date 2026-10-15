# Feature Specification: Task-Based Model Guidance

**Feature Branch**: `038-model-routing-policy`

**Created**: 2026-10-15

**Status**: Draft

**Input**: FIFO item 13: let teams assign model preferences to SDD tasks or phases, using stronger models for complex implementation and lower-cost models for routine work, across Claude Code, VS Code/Copilot, Codex, and other hosts where supported.

## User Scenarios & Testing

### User Story 1 - Define model preferences by work type (Priority: P1)

A team lead wants one reviewable policy that says which model capability or named model to prefer for specification, planning, implementation, tests, review, and routine documentation tasks.

**Independent Test**: Create a sample policy, validate it, and render a stable report showing each task profile, quality tier, host-specific model, and rationale.

**Acceptance Scenarios**:

1. **AC-001** Given a valid policy with profiles for multiple task types, when BrownPants validates and reports it, then the output preserves each profile, task category, quality tier, rationale, and host-specific model in stable order.
2. **AC-002** Given a policy that assigns different model tiers to implementation and routine documentation, when it is rendered, then both distinctions are visible and the report does not replace user-provided model names with BrownPants recommendations.
3. **AC-003** Given an invalid policy, including an unknown task type, missing model preference, duplicate assignment, or malformed input, when validation runs, then BrownPants reports the path and actionable issue and returns a non-success status.
4. **AC-011** Given a valid policy, when the same report is requested as Markdown and JSON, then both formats contain equivalent profile names, task categories, tiers, model identifiers, and rationales.

### User Story 2 - Understand host support before relying on a policy (Priority: P1)

A developer using Visual Studio, VS Code/Copilot, Claude Code, Codex, or another host wants to know whether a task profile can apply its model preference, or whether they must select it manually.

**Independent Test**: Export the same policy to each supported host format and verify model declarations and limitations without requiring sign-in, network calls, or launching a host.

**Acceptance Scenarios**:

1. **AC-004** Given a valid policy, when BrownPants exports VS Code/Copilot, Visual Studio 18.4+, Claude Code, or Codex profiles, then the generated native files contain the configured user model for each selected task profile and install only under that host's documented project-local agent directory; Visual Studio and VS Code exports use distinct profile filenames under their shared `.github/agents/` directory.
2. **AC-005** Given a host or model identifier that BrownPants cannot verify, when reporting the assignment, then BrownPants preserves the user's value and labels support as unverified rather than claiming it is honored.
3. **AC-006** Given an existing generated profile with identical content, when an export repeats, then BrownPants reports it as current; given a conflicting file, then it preserves the file and reports an actionable conflict.

### User Story 3 - Keep model assignment optional and safe (Priority: P2)

A team wants to adopt task-based model guidance without changing their SDD engine, sending source code to a model provider, or forcing a particular vendor.

**Independent Test**: Run the command with no policy, with an unknown optional field, and in a repository with no model tooling; verify read-only defaults and clear behavior.

**Acceptance Scenarios**:

1. **AC-007** Given no policy file, when validation or reporting runs, then BrownPants reports that no assignments are configured and does not create or modify files.
2. **AC-008** Given a valid policy, when it is validated or exported, then BrownPants performs no model/API call, network request, project evaluation, or write outside the explicitly selected host agent directory.
3. **AC-009** Given an export command without an explicit host, an unsupported host, or a missing host-specific model, when it runs, then BrownPants refuses to guess and reports the missing selection without writing files.
4. **AC-010** Given host capabilities change over time, when docs describe them, then dated evidence links are provided and the policy remains usable as user-owned preferences independent of BrownPants' capability catalog.

## Edge Cases

- The policy file is absent, unreadable, oversized, malformed, or contains duplicate or unknown entries.
- A requested host is unknown, unavailable, or does not support per-task model selection.
- A provider renames, removes, or restricts a configured model; BrownPants cannot authenticate to verify availability.
- A team prefers capability tiers rather than vendor model IDs, or mixes both.
- A host ignores instructions or selects a different model at runtime.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST support an opt-in, repository-local policy for expressing user-selected model preferences by SDD phase or task category.
- **FR-002**: Each profile MUST identify one or more task categories, a user-selected quality tier, host-specific model identifiers, and a short rationale.
- **FR-003**: BrownPants MUST validate policy structure and report actionable, source-located errors for invalid entries.
- **FR-004**: BrownPants MUST render policy assignments in deterministic Markdown and JSON.
- **FR-005**: BrownPants MUST generate project-local native agent profiles for VS Code/Copilot, Visual Studio 18.4+, Claude Code, and Codex using documented model-selection fields, without changing host-global settings.
- **FR-006**: BrownPants MUST state that generated profiles configure a model when that agent is selected or invoked but do not guarantee that an external host will invoke the profile for a matching task.
- **FR-007**: BrownPants MUST preserve exact model names supplied by the team and MUST NOT choose, download, authenticate to, or invoke a model.
- **FR-008**: Export MUST require an explicit host, remain within the corresponding project-local agent directory, be idempotent for identical files, and preserve conflicting files.
- **FR-009**: The model policy MUST remain optional and independent of a specific SDD engine, model provider, subscription, or network service.
- **FR-010**: Documentation MUST explain that model capabilities, access, pricing, and host behavior vary and can change; model choice is a team policy, not a quality guarantee.

The default policy path is `.brownpants/models.json`. Commands may accept an explicit policy path for fixture and alternate-layout use, but the policy cannot direct BrownPants to read files outside the selected repository root.

## Key Entities

- **Task assignment**: A named SDD phase or task category, selected model preference, rationale, and host-specific instruction.
- **Model preference**: A capability tier or exact provider/model identifier supplied by the team.
- **Host support status**: Evidence-bounded description of whether a host can apply, expose, or only be instructed about an assignment.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A team can validate and inspect its configured task assignments in one local command without authenticating to a model provider.
- **SC-002**: 100% of configured assignments appear in stable Markdown and JSON output with the same model preference and rationale.
- **SC-003**: Generated profiles for the four supported hosts contain the user-configured model identifier, and documentation explains that profile invocation is still controlled by the host or user.
- **SC-004**: Invalid configuration reports at least the offending file and entry and exits unsuccessfully; validation does not modify repository files and export writes only requested host profiles.
- **SC-005**: The feature adds no runtime NuGet dependency and makes no network or model-provider requests.

## Assumptions

- Teams choose exact model identifiers and capability tiers based on their own quality, cost, privacy, and access constraints.
- Host support changes over time; compatibility guidance will cite official host documentation and be rechecked during maintenance.
- BrownPants can generate native per-task agent profiles for supported hosts, but host delegation, user selection, model availability, and runtime fallback remain outside BrownPants' control.
- User-supplied repository configuration is untrusted data and must not direct file reads outside the repository or trigger execution.
