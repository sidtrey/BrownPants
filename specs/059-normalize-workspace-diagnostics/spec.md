# Feature Specification: Normalize Workspace Diagnostics

**Feature Branch**: `059-normalize-workspace-diagnostics`
**Created**: 2026-10-15
**Status**: Draft
**Input**: BrownPants-on-BrownPants semantic inspection showed an MSBuild workspace issue with an absolute checkout path, making otherwise useful diagnostics machine-specific and unsafe to share.

## User Scenarios & Testing

### User Story 1 - Share actionable semantic diagnostics safely (Priority: P1)

A maintainer reviewing semantic analysis needs the actual project-loading reason and a repository-relative project path, without exposing the machine-specific checkout location. Two teammates analyzing the same repository from different directories should receive equivalent diagnostics.

**Why this priority**: Brownfield analysis depends on explicit limits, and paths from a developer machine make reports harder to compare and share.

**Independent Test**: Normalize representative workspace messages containing in-repository and external absolute paths, serialize semantic output, and compare results created from two different checkout roots.

**Acceptance Scenarios**:

1. **AC-001** **Given** a workspace diagnostic names a project inside the scanned repository by absolute path, **When** BrownPants reports it, **Then** the diagnostic reason remains visible and the project path is repository-relative.
2. **AC-002** **Given** a diagnostic contains an absolute path outside the repository, **When** BrownPants reports it, **Then** the external path is replaced with a stable marker while the diagnostic explanation remains useful.
3. **AC-003** **Given** identical source trees under two different absolute checkout roots, **When** semantic diagnostics are produced, **Then** their normalized messages are identical.
4. **AC-004** **Given** a workspace failure is normalized, **When** semantic JSON and impact completeness are reported, **Then** existing fields, counts, and incomplete status remain unchanged.

## Edge Cases

- Windows and Unix path separators appear in diagnostic text.
- The repository root appears as a prefix of a similarly named external path.
- A diagnostic has no path or has multiple path references.
- A workspace issue is omitted by the caller-selected sample limit.

## Requirements

### Functional Requirements

- **FR-001**: Repository-contained absolute paths in workspace diagnostic messages MUST be rewritten as normalized repository-relative paths.
- **FR-002**: Absolute paths outside the scanned repository MUST NOT be emitted in workspace diagnostic messages; replace them with a stable external-path marker.
- **FR-003**: Normalization MUST preserve the diagnostic's actionable reason and deterministic ordering.
- **FR-004**: Normalization MUST be applied before semantic diagnostics are exposed to Markdown, JSON, evidence, or impact reports.
- **FR-005**: Diagnostic counts, completeness decisions, and existing JSON schema fields MUST remain unchanged.
- **FR-006**: Path handling MUST distinguish the repository root from longer paths that merely share its string prefix.

## Key Entities

- **Workspace diagnostic**: A bounded MSBuild or Roslyn workspace issue containing explanatory text and, optionally, source/project paths.

## Success Criteria

- **SC-001**: No semantic workspace diagnostic contains the current checkout's absolute root.
- **SC-002**: Repository-local diagnostic paths use stable `/` separators and remain inside the repository.
- **SC-003**: Equivalent diagnostics from different checkout roots serialize identically, with reasons and completeness counts preserved.
- **SC-004**: External absolute paths are hidden behind a stable marker without discarding the diagnostic reason.

## Assumptions

- Semantic workspace messages are diagnostic text, not a stable machine-readable API; the public shape remains a string list.
- The repository root is the only trusted base for path disclosure; outside paths are not needed to understand whether analysis was incomplete.
