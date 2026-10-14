# Feature Specification: Pluggable SDD Discovery

**Feature Branch**: `005-pluggable-sdd-discovery`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Allow additional spec-driven development engines to declare repository artifact roots for shared inventory reporting."

## User Scenarios & Testing

### User Story 1 - Register another SDD engine (Priority: P1)

A repository maintainer using an SDD engine beyond Spec Kit or OpenSpec describes that engine's artifact, specification, and change directories in a repository-local declaration. Running BrownPants then reports the engine alongside built-in workflows, allowing any planning tool to consume the same JSON or Markdown evidence.

**Why this priority**: A stable, user-extensible inventory is the practical path from supporting two engines to serving repositories using other SDD systems without adding engine-specific code to BrownPants.

**Independent Test**: Scan a temporary repository containing one custom definition and sample artifacts; confirm its totals and paths appear in JSON and Markdown while the built-in workflow rows remain present.

**Acceptance Scenarios**:

1. **Given** a valid custom workflow definition and files under its artifact roots, **When** the repository is inspected, **Then** BrownPants reports its name, initialization state, specification directory count, configured active change count, total artifact count, and deterministic bounded artifact path sample.
2. **Given** a custom workflow with no change roots, **When** the repository is inspected, **Then** its active change count is absent rather than reported as zero.
3. **Given** overlapping artifact roots or duplicate discovered paths, **When** the repository is inspected, **Then** each repository file contributes once to the workflow artifact total and sample.

### User Story 2 - Diagnose invalid declarations safely (Priority: P1)

A maintainer receives actionable diagnostics when a custom definition is malformed or unsafe, and can correct it without BrownPants scanning outside the repository or silently omitting the workflow.

**Why this priority**: Users need to trust the inventory and must not be able to use configuration to inspect paths outside the selected repository.

**Independent Test**: Scan temporary repositories with malformed JSON, duplicate names, unsupported schema versions, and unsafe roots; verify diagnostics identify the problem and no outside files are reported.

**Acceptance Scenarios**:

1. **Given** malformed or unsupported configuration, **When** the repository is inspected, **Then** JSON and Markdown include a diagnostic and built-in workflow analysis still completes.
2. **Given** an absolute path, parent traversal, wildcard, or empty required root, **When** the declaration is inspected, **Then** BrownPants rejects that entry and never reads or reports files outside the repository.
3. **Given** a custom name that collides case-insensitively with a built-in or another custom workflow, **When** the repository is inspected, **Then** the conflicting entry is diagnosed and does not create duplicate workflow rows.

### Edge Cases

- No custom declaration exists; built-in Spec Kit and OpenSpec results and existing report behavior remain unchanged.
- A valid declaration references roots that contain no files; the workflow is reported as not initialized with zero artifacts.
- A directory is listed in multiple categories or under overlapping roots; totals and samples stay unique and stable.
- Archive directory names are compared case-insensitively and apply only to the first-level directory below a configured change root.
- The sample limit is one or exceeds the available artifact count; the CLI rejects zero.
- A root path differs in case from discovered paths on a case-insensitive filesystem; matching remains consistent with the scanner's path semantics.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST allow repository maintainers to declare additional SDD workflows without changing BrownPants source code.
- **FR-002**: Each declaration MUST contain a non-empty workflow name and one or more repository-relative artifact roots; specification roots, change roots, and archived change directory names MAY be provided.
- **FR-003**: BrownPants MUST report each declared workflow through the shared SDD inventory fields and the existing JSON and Markdown output formats.
- **FR-004**: Artifact totals and path samples MUST be based on unique repository-relative files, sorted deterministically, and bounded by the inspect sample limit.
- **FR-005**: BrownPants MUST report an optional active change count only when change roots are configured, excluding configured archive directory names.
- **FR-006**: Root values MUST be normalized repository-relative directory paths and MUST reject absolute paths, parent traversal, wildcards, empty values, and invalid segments. BrownPants MUST match only the scanner's discovered in-root file list and MUST NOT enumerate or read configured roots.
- **FR-007**: Invalid configuration and conflicting workflow names MUST produce actionable report diagnostics; they MUST NOT prevent built-in workflow analysis from completing.
- **FR-008**: BrownPants MUST reject duplicate workflow names case-insensitively, including collisions with built-in workflow names.
- **FR-009**: Custom workflow configuration MUST be declarative and MUST NOT execute scripts, load assemblies, access the network, or introduce a runtime plugin dependency.
- **FR-010**: When no custom definition is present, existing Spec Kit and OpenSpec discovery results MUST remain unchanged.
- **FR-011**: The declaration MUST use schema version 1, contain a non-empty workflow list, reject unknown fields, and be limited to 1 MiB.
- **FR-012**: A declaration MUST contain no more than 100 workflow definitions.

### Key Entities

- **Custom workflow definition**: A repository-local declaration containing a unique display name, required artifact roots, optional specification and change roots, and optional archive directory names.
- **SDD workflow inventory**: The shared report row describing initialization, discovered specification directories, optional active changes, artifact totals, and bounded paths.
- **Configuration diagnostic**: A deterministic message identifying an invalid definition or field so a maintainer can fix it.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A maintainer can add one supported custom workflow by editing only repository configuration; no BrownPants rebuild or source change is required.
- **SC-002**: For valid fixtures, JSON and Markdown show the same custom workflow totals and sampled paths, with zero duplicate file entries.
- **SC-003**: Every invalid configuration fixture yields a diagnostic while built-in workflow inventory remains available.
- **SC-004**: Adversarial absolute and traversal roots produce zero reported files from outside the scanned repository.
- **SC-005**: Repeated scans of the same fixture produce byte-for-byte identical workflow inventory and diagnostics.

## Assumptions

- Custom workflow declarations are repository configuration and are reviewed as part of the codebase.
- The first version supports one BrownPants-owned declaration file and directory roots; it does not discover or interpret arbitrary third-party configuration formats.
- Engine-specific lifecycle hooks remain the responsibility of adapters under `integrations/`; this feature extends inventory discovery only.
- Existing path exclusion rules continue to apply to custom roots.
