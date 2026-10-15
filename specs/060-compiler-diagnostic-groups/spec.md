# Feature Specification: Compiler Diagnostic Groups

**Feature Branch**: `060-compiler-diagnostic-groups`
**Created**: 2026-10-15
**Status**: Draft
**Input**: BrownPants-on-BrownPants semantic analysis reported 21 compiler errors as individual samples: 12 shared the same project and diagnostic code. Grouping these counts can make brownfield failures easier to triage while retaining the source-level examples.

## User Scenarios & Testing

### User Story 1 - Triage repeated compiler errors (Priority: P1)

A developer reviewing an unfamiliar or legacy project needs to see which projects and compiler diagnostic codes account for the most reported errors, then inspect individual source-located samples without mistaking frequency for a diagnosis.

**Why this priority**: In brownfield projects, one unavailable reference or framework can produce many related diagnostics. A compact distribution helps reviewers decide where to investigate first, while raw samples preserve evidence.

**Independent Test**: Analyze compilations containing multiple diagnostic codes in multiple projects, serialize the result, and verify group totals, deterministic ordering, bounded samples, and unchanged raw diagnostic counts.

**Acceptance Scenarios**:

1. **AC-001** **Given** compiler errors share a project and diagnostic code, **When** semantic results are reported, **Then** a group shows the exact count for that project/code pair.
2. **AC-002** **Given** a compiler error group and sampled diagnostics, **When** a user reads Markdown or JSON, **Then** the group summary and raw source-located samples are both available and no root cause is asserted by the summary.
3. **AC-003** **Given** identical diagnostics are encountered in different compilation enumeration orders, **When** reports are produced, **Then** group order and representative group values are identical.
4. **AC-004** **Given** the configured sample limit is smaller than the number of groups or diagnostics, **When** semantic output is produced, **Then** total error and group counts remain exact and omitted group/sample counts are reported.

## Edge Cases

- A compilation has no errors or no source location for an error.
- A diagnostic code occurs in more than one project.
- Multiple distinct messages share one project and code; the group must not imply their messages are identical.
- The diagnostic group sample limit is reached while raw diagnostic sampling has a different omission count.

## Requirements

### Functional Requirements

- **FR-001**: Semantic analysis MUST count compiler errors by project path and diagnostic code across all analyzed compilations, independently of sampled diagnostic rows.
- **FR-002**: Semantic JSON MUST expose bounded group samples, exact total group count, and exact omitted group count as additive fields.
- **FR-003**: Markdown MUST show the most useful compiler diagnostic groups before or alongside the existing source-located samples.
- **FR-004**: Group ordering MUST be deterministic by normalized project path and diagnostic code, regardless of compilation enumeration order.
- **FR-005**: Existing compiler error totals, omitted sample counts, diagnostic rows, and public schema version MUST remain unchanged.
- **FR-006**: Group summaries MUST describe counts only; they MUST NOT infer causes, severity beyond the compiler diagnostic, or fixes.
- **FR-007**: Group sample storage MUST be bounded by the existing user-selected sample limit.

## Key Entities

- **Compiler diagnostic group**: A project-relative project path and compiler diagnostic code with an exact count among analyzed errors.
- **Compiler diagnostic sample**: The existing source-located project, file, line, code, and message sample.

## Success Criteria

- **SC-001**: BrownPants' current semantic report groups its 21 compiler errors into four project/code rows with counts 12, 4, 3, and 2, while preserving the 21-error total and individual diagnostic samples.
- **SC-002**: Tests prove exact counts across multiple projects, stable ordering across input permutations, and exact omitted counts when group and diagnostic samples are limited.
- **SC-003**: Existing consumers retain the same schema version and existing diagnostic fields; the new group fields are additive.
- **SC-004**: Markdown presents group counts without claiming a causal explanation for the diagnostics.

## Assumptions

- A group key is the normalized project path plus diagnostic code; different message text can occur within a group.
- The existing `--limit` governs both compiler group samples and raw compiler diagnostic samples, with each omission count reported independently.
- Compiler errors remain evidence from the loaded project configuration, not proof that source is defective or that a proposed fix is correct.
