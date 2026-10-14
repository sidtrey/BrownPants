# Feature Specification: Report Python Factory and DI Signals

**Feature Branch**: `013-python-architecture-signals`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User goal: provide useful brownfield evidence for any codebase, including factory and dependency-injection patterns outside .NET.

## User Scenarios & Testing

### User Story 1 - Find Python construction and dependency wiring (Priority: P1)

A developer inspecting a Python repository wants source-located leads to factory declarations and common dependency-injection conventions so they can trace how services are built and supplied.

**Independent Test**: Scan temporary `.py` files containing factory names, provider registrations, and FastAPI dependencies; verify candidate paths, lines, language coverage, and filtering of comments and strings in Markdown and JSON.

**Acceptance Scenarios**:

1. **Given** a Python factory or recognized DI convention, **When** the repository is inspected, **Then** BrownPants reports a source-located `Candidate` signal labeled `Python`.
2. **Given** pattern text only in a comment, single-line string, or multiline string, **When** the repository is inspected, **Then** it produces no signal.
3. **Given** a repository with Python source, **When** language coverage is reported, **Then** Python is labeled as lexical candidate analysis.

## Edge Cases

- Comments follow code on the same line.
- Triple-quoted strings span lines and can use either quote character.
- Raw, bytes, and formatted string prefixes do not change string filtering.
- Python files that contain no recognized pattern remain inventoried without architecture signals.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST report source-located candidates for Python classes whose names identify them as factories and factory methods with common create/build/make/resolve names.
- **FR-002**: BrownPants MUST recognize common dependency wiring evidence: Dependency Injector provider declarations, FastAPI `Depends`, `Provide[...]`, and `@inject`.
- **FR-003**: Each signal MUST identify `Python`, a repository-relative path, the source line, and `Candidate` evidence.
- **FR-004**: Matching MUST ignore Python comments and string literals, including triple-quoted strings.
- **FR-005**: Python source coverage MUST report `Lexical candidate patterns` when these signals are enabled.
- **FR-006**: Markdown and JSON MUST expose equivalent signal records, with deterministic ordering and bounded samples.
- **FR-007**: Documentation MUST state that patterns are lexical candidates and do not prove framework use, resolved dependencies, or runtime behavior.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Representative Python factory and DI fixtures appear with expected category, path, and line in both report formats.
- **SC-002**: Comment and string-only fixtures produce zero Python architecture signals.
- **SC-003**: Existing C#, VB.NET, and native candidate fixtures pass unchanged.
- **SC-004**: Repeated scans of identical Python fixtures produce byte-identical Markdown and JSON.

## Assumptions

- Python analysis is intentionally lexical and dependency-free; no Python interpreter or package installation is required.
- Pattern names are a bounded set of conventions, not framework detection or semantic relationship resolution.
