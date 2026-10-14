# Feature Specification: Cross-Ecosystem Repository Inventory

**Feature Branch**: `002-cross-ecosystem-inventory`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Make BrownPants work with any codebase, while prioritizing deep .NET and C#/MFC analysis."

## User Scenarios & Testing

### User Story 1 - Orient in any repository (Priority: P1)

A developer points BrownPants at a repository in an unfamiliar language and needs a reliable map even when BrownPants has no source parser for that language.

**Why this priority**: A language-neutral inventory is the minimum useful behavior for every codebase and prevents the tool from appearing .NET-only.

**Independent Test**: Scan a fixture containing recognized and unknown file extensions plus common build manifests; verify all files contribute to deterministic inventory and manifest evidence.

**Acceptance Scenarios**:

1. **Given** a repository with source files from multiple languages, **When** it is inspected, **Then** counts and paths include all readable files, including unfamiliar and extensionless files.
2. **Given** an unfamiliar language with no specialized analyzer, **When** it is inspected, **Then** BrownPants still reports its file types and project markers without implying that it parsed the source.

### User Story 2 - Identify project ecosystems (Priority: P1)

A developer needs to see which project and build ecosystems are present, including when a repository combines several ecosystems.

**Why this priority**: Manifest evidence helps Spec Kit plan against the repository's actual build context instead of assuming a single language or toolchain.

**Independent Test**: Scan a mixed fixture with representative manifests from several ecosystems and verify stable ecosystem names and repository-relative evidence paths.

**Acceptance Scenarios**:

1. **Given** common .NET, native C/C++, Python, JavaScript, Go, Rust, JVM, Ruby, PHP, Apple, or Dart manifests, **When** the repository is inspected, **Then** each detected ecosystem is named with the manifest paths that support it.
2. **Given** multiple manifests for one ecosystem or a repository with multiple ecosystems, **When** the report is generated, **Then** evidence is grouped deterministically without losing individual paths.
3. **Given** a file that is not a project manifest, **When** the repository is inspected, **Then** its extension contributes to file inventory without being mislabeled as a build system.

### User Story 3 - Understand analysis coverage (Priority: P2)

A developer needs to distinguish general inventory from source-level analysis so unsupported languages are not mistaken for parsed or semantically resolved code.

**Why this priority**: Honest coverage boundaries preserve trust while language analyzers are added incrementally.

**Independent Test**: Inspect a mixed repository containing supported and inventory-only languages and verify the report identifies both the evidence and its limits.

**Acceptance Scenarios**:

1. **Given** source analyzed by a specialized parser, **When** the report is generated, **Then** its analysis section names the language and evidence type.
2. **Given** source that is only inventoried, **When** the report is generated, **Then** the report does not claim syntax or semantic analysis for it.

### Edge Cases

- Repositories may have no recognized manifest, only extensionless files, or mixed and nested ecosystems.
- Lockfiles may identify a package manager but are not by themselves proof that source was analyzed.
- Malformed manifests must not prevent inventory of other files; report the path and limitation when parsing is attempted.
- Generated, dependency, and VCS directories must remain bounded and excluded consistently.

## Requirements

### Functional Requirements

- **FR-001**: Inspection MUST inventory readable files with extensions and counts, preserving extensionless and unknown file evidence.
- **FR-002**: Inspection MUST detect and classify common project/build manifests across .NET, native C/C++, Python, JavaScript/TypeScript, Go, Rust, JVM, Ruby, PHP, Apple, and Dart ecosystems.
- **FR-003**: Each ecosystem finding MUST include repository-relative supporting paths; multiple findings MUST have stable ordering and no duplicated paths.
- **FR-004**: Markdown and JSON MUST expose equivalent project ecosystem and file inventory evidence.
- **FR-005**: Reports MUST distinguish project inventory, syntax analysis, and semantic analysis, and identify languages that are inventory-only.
- **FR-006**: Missing, unknown, or malformed manifests MUST NOT prevent scanning other readable files.
- **FR-007**: Default inspection MUST remain offline and MUST NOT build projects, execute project scripts, or evaluate arbitrary build targets.
- **FR-008**: Repeated scans of unchanged input MUST produce identical findings and ordering.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Fixtures for each listed ecosystem produce a correctly named ecosystem and source-located manifest evidence.
- **SC-002**: Mixed and unknown-language fixtures retain 100% of readable test files in file/extension inventory.
- **SC-003**: Markdown and JSON contain the same ecosystem findings and evidence paths.
- **SC-004**: Repeated scans of an unchanged fixture produce byte-identical Markdown and JSON output.
- **SC-005**: Reports never describe inventory-only languages as syntax- or semantically analyzed.

## Assumptions

- The general inventory applies to every repository and is independent of the SDD engine consuming it; specialized source analysis remains an explicit, growing set of capabilities.
- Spec Kit is the first integrated workflow adapter; other engines can consume the same reports or add separate adapters.
- Initial ecosystem detection is based on conventional manifest paths and file names, not dependency resolution.
- Deep .NET, C#/VB Roslyn, native MFC/COM, and C++/CLI evidence remains a priority within the broader inventory.
- Unknown languages remain useful through file, path, and manifest evidence even before a specialized analyzer exists.
