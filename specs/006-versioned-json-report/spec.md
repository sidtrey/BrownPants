# Feature Specification: Versioned JSON Report

**Feature Branch**: `006-versioned-json-report`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Give machine-readable BrownPants inventory reports an explicit versioned contract so SDD engines can detect compatible report formats."

## User Scenarios & Testing

### User Story 1 - Identify the report contract (Priority: P1)

A planning tool or repository maintainer reads a BrownPants JSON inventory and identifies which report contract it follows before consuming inventory fields. The same version marker is present whether the repository contains built-in SDD artifacts, custom SDD artifacts, or no SDD artifacts.

**Why this priority**: Other SDD engines need a stable way to choose a compatible reader as BrownPants evolves.

**Independent Test**: Run successful JSON inspections against fixtures with and without SDD configuration; parse each output and verify the same root version and documented inventory structure.

**Acceptance Scenarios**:

1. **Given** a successful JSON inspection, **When** a consumer reads the report root, **Then** it finds integer `SchemaVersion` equal to `1` before processing inventory fields.
2. **Given** a repository with no SDD workflow artifacts, **When** it is inspected as JSON, **Then** `SchemaVersion` is still present and equals `1`.
3. **Given** a successful Markdown inspection, **When** a person reads the report, **Then** no JSON schema marker is added to the human-focused report.

### Edge Cases

- Semantic-mode JSON reports use the same schema version as default JSON reports.
- Reports with custom SDD workflows and configuration diagnostics retain the same root version.
- Existing JSON consumers that ignore unknown root properties continue to read the existing fields.
- Future breaking report changes receive a new integer schema version; additive optional fields remain in version 1.

## Requirements

### Functional Requirements

- **FR-001**: Every successfully generated JSON inventory MUST include an integer `SchemaVersion` root property with value `1`.
- **FR-002**: The version marker MUST be part of the same JSON object as the existing `RepositoryInventory` fields.
- **FR-003**: Markdown output MUST remain free of the JSON version marker.
- **FR-004**: The repository MUST document the fields and compatibility meaning of report schema version 1 for tool authors.
- **FR-005**: Additive optional JSON fields MAY be introduced within version 1; removing or renaming existing fields, changing field types, or changing their meaning MUST increment the schema version.
- **FR-006**: The schema version MUST remain identical across default, semantic, empty-repository, and SDD-configured JSON reports.
- **FR-007**: The version marker MUST be a constant of the report contract and MUST NOT derive from package version, runtime version, repository contents, or timestamps.

### Key Entities

- **JSON report contract**: The versioned root object consumed by tools, containing the schema version and repository inventory fields.
- **Schema version**: An integer that identifies compatibility of JSON field names, types, and meanings independently of BrownPants package releases.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Every successful JSON CLI scenario in the acceptance suite exposes `SchemaVersion: 1` at the root.
- **SC-002**: A consumer can determine report compatibility from one root property without inspecting package metadata or report contents.
- **SC-003**: Markdown output does not gain a JSON-only schema marker.
- **SC-004**: The change adds no runtime package dependency and leaves the existing inventory fields unchanged.

## Assumptions

- The initial published JSON contract is version 1; no external version marker exists today.
- The version identifies the serialized report contract, not the BrownPants NuGet package or CLI release.
- This feature documents the current report contract and adds its version marker; it does not introduce an external JSON Schema validator.
