# Feature Specification: Semantic Source Path Boundaries

**Feature Branch**: `027-semantic-path-boundaries`

**Created**: 2026-10-14

**Status**: Draft

**Input**: BrownPants-on-BrownPants semantic inspection found a generated `Microsoft.NET.Test.Sdk.Program.cs` declaration under the local NuGet cache in the repository report.

## User Scenarios & Testing

### User Story 1 - Keep semantic reports scoped to the inspected repository (Priority: P1)

A developer opts into Roslyn semantic analysis and expects source locations in the report to identify code in the scanned repository, not generated files from a local package cache or another project directory.

**Why this priority**: Machine-specific paths make reports non-portable and can disclose local usernames, directory structure, and package locations. External generated code is not repository-owned evidence.

**Independent Test**: Analyze a compilation containing one repository source tree and one source tree outside the repository; verify declarations and invocation sites are scoped to the repository, while a call to an external source method remains resolved without an external target location.

**Acceptance Scenarios**:

1. **AC-001** — **Given** a compilation with repository and external source trees, **When** semantic analysis runs, **Then** declarations, invocation sites, and their counts include only nodes from repository-contained source trees.
2. **AC-002** — **Given** a repository invocation resolved to a method declared in an external source tree, **When** the call is reported, **Then** its resolved target symbol remains available and its `TargetPath` and `TargetLine` are null.
3. **AC-003** — **Given** semantic findings for a repository, **When** any finding is emitted, **Then** every non-null project or source path is repository-relative, uses `/` separators, and contains no rooted path or parent-directory segment.
4. **AC-004** — **Given** the BrownPants repository and its locally restored test SDK, **When** `inspect --semantic` emits Markdown or JSON, **Then** reports do not expose the package-cache source path and retain the repository's own declarations and resolved call counts.

## Edge Cases

- A source tree uses a relative path, an absolute path, or a path outside the repository root.
- A resolved method has metadata-only locations or both in-repository and external source locations.
- A loaded solution contains a project outside the scanned repository.
- A project includes generated source beneath its own repository root; it remains eligible because it is inside the inspected repository.

## Requirements

### Functional Requirements

- **FR-001**: Semantic declarations, resolved-call sites, and unresolved-call sites MUST be collected only from source trees whose normalized paths are inside the inspected repository root.
- **FR-002**: Repository-contained source call sites MUST retain semantic resolution to external methods, but external target source locations MUST NOT be emitted.
- **FR-003**: Every non-null project, source, and target path in semantic records MUST be repository-relative, slash-normalized, and free of parent traversal segments.
- **FR-004**: Semantic counts MUST describe only analyzed repository source trees; MSBuild project-load counts MUST continue to describe repository projects processed by the analyzer.
- **FR-005**: Existing in-repository C# and Visual Basic semantic resolution and call-impact behavior MUST remain available.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A fixture with an in-repository tree and an external tree reports only in-repository declarations and call sites, with matching semantic counts.
- **SC-002**: A call from repository code to an external source declaration remains resolved while exposing no external target path or line.
- **SC-003**: All semantic path fields in the fixture contain only safe repository-relative paths on Windows and Unix-like hosts.
- **SC-004**: BrownPants-on-BrownPants Markdown and JSON semantic reports contain no local NuGet cache path and retain resolved repository call findings.

## Assumptions

- Roslyn may use external source trees for binding, but those trees are dependencies rather than repository findings.
- This feature does not alter MSBuild evaluation behavior, semantic completeness claims, or JSON schema version.
