# Feature Specification: Managed and Native Interop Analysis

**Feature Branch**: `001-managed-native-interop`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Help developers understand how legacy managed client code reaches native MFC components and how factories and dependency injection create or supply dependencies. Support the languages and project types found in mixed .NET/native repositories, especially C#, VB.NET, and C++.

## User Scenarios & Testing

### User Story 1 - Trace managed-to-native boundaries (Priority: P1)

A developer inspecting a legacy client needs to find likely transitions from C# or VB.NET into native code, including COM activation, P/Invoke, and C++/CLI bridges, and determine which evidence is direct and which needs manual confirmation.

**Why this priority**: Following the boundary is the first step in understanding behavior spread across managed and native components.

**Independent Test**: Scan a small mixed-language fixture and verify that each supported boundary signal includes its file and line, while unresolved targets are labeled as candidates.

**Acceptance Scenarios**:

1. **Given** managed source containing representative COM, P/Invoke, and C++/CLI evidence, **When** the repository is inspected, **Then** the report lists each signal with a source location and evidence type.
2. **Given** a boundary whose target cannot be statically determined, **When** the report is generated, **Then** it marks the target unresolved instead of asserting a call relationship.

### User Story 2 - Find construction and dependency wiring (Priority: P2)

A developer needs to locate factory methods and delegates, constructor-injected dependencies, and dependency-injection registration or resolution sites that explain how application services and native-facing objects are created.

**Why this priority**: Creation and wiring patterns explain runtime behavior after relevant components have been found.

**Independent Test**: Scan fixtures containing common factory, constructor-injection, and DI patterns in C#, VB.NET, and native source; verify source-located signals and clear evidence labels.

**Acceptance Scenarios**:

1. **Given** a factory method or delegate, constructor-injected dependency, or DI registration/resolution pattern in source code, **When** the repository is inspected, **Then** the report identifies it as a candidate and includes its source location.
2. **Given** a `void` C# method or VB.NET `Sub` named `Create`, `Build`, or `Resolve`, **When** the repository is inspected, **Then** it is not reported as a factory method because it does not return a created value.
2. **Given** matching text only in comments or string literals, **When** the report is generated, **Then** it does not report that text as executable source evidence.

### User Story 3 - Review older projects without a successful build (Priority: P3)

A developer needs useful structural and source evidence from a repository whose legacy projects cannot be built on the current machine.

**Why this priority**: Build prerequisites often prevent initial assessment of old .NET and native code.

**Independent Test**: Inspect a fixture with malformed or unavailable project references and verify that available inventory and source signals remain usable and limitations are reported.

**Acceptance Scenarios**:

1. **Given** an unbuildable legacy project with readable source, **When** the default inspection runs, **Then** it produces available inventory and source-local findings without requiring a successful build.
2. **Given** a project or file that cannot be analyzed, **When** the report is generated, **Then** it identifies the omission or limitation.

### Edge Cases

- Missing, malformed, or unsupported project files must not prevent analysis of other readable projects.
- Dynamic loading, reflection, macros, generated code, and runtime registration may hide relationships; the report must leave these unresolved when evidence is insufficient.
- Comments, strings, and unrelated method names that resemble patterns must not be presented as confirmed relationships.
- Repeated scans of unchanged input must produce stable ordering and content.

## Requirements

### Functional Requirements

- **FR-001**: The tool MUST inventory relevant managed and native project files, including C#, VB.NET, and native C++ projects, and report MFC or CLR project configuration when present.
- **FR-002**: The tool MUST identify source-located candidate signals for managed/native boundaries, including COM, P/Invoke, and C++/CLI patterns.
- **FR-003**: The tool MUST identify source-located value-returning factory method declarations and factory delegates, constructor-injected dependencies, and dependency-injection registration or resolution signals in supported source languages.
- **FR-004**: The tool MUST distinguish direct or resolved evidence from heuristic candidates and unresolved relationships.
- **FR-005**: The default inspection MUST provide useful results without building the inspected repository or requiring legacy toolchains.
- **FR-006**: The tool MUST avoid treating comments and string literals as executable pattern evidence where its parser can distinguish them.
- **FR-007**: Reports MUST include repository-relative source paths and line locations where available, stable ordering, and explicit analysis limitations.
- **FR-008**: Markdown and JSON reports MUST communicate the same findings and evidence status.
- **FR-009**: The tool MUST continue analyzing readable projects when another project or source file is malformed or unsupported.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Representative fixtures for COM, P/Invoke, C++/CLI, MFC project metadata, factory methods and delegates, constructor injection, and DI produce correctly categorized findings with source locations.
- **SC-002**: Fixture patterns appearing only in comments or strings produce no executable-source findings.
- **SC-003**: A fixture with unavailable references or an unbuildable legacy project still yields inventory and findings from readable files.
- **SC-004**: Two scans of unchanged input produce identical Markdown and JSON findings, including ordering.
- **SC-005**: Every finding that is not statically resolved is visibly identified as a candidate or unresolved signal.

## Assumptions

- The primary users are developers assessing older business applications, often with C# clients interacting with MFC through native interop layers.
- Initial language coverage focuses on C#, VB.NET, and C++; other languages may receive project inventory but not source-level pattern analysis until explicitly supported.
- Static source evidence cannot prove all runtime behavior; reflection, dynamic loading, generated code, and complex macro expansion may remain unresolved.
- The default workflow inspects files and project metadata without executing builds or arbitrary project targets.
