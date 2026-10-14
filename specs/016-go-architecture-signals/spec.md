# Feature Specification: Report Go Factory and Dependency-Injection Signals

**Feature Branch**: `016-go-architecture-signals`

**Created**: 2026-10-14

**Status**: Draft

**Input**: BrownPants should provide useful, source-located architecture evidence across codebases, including Go repositories using constructor functions and dependency-injection frameworks.

## User Scenarios & Testing

### User Story 1 - Find Go construction and wiring conventions (Priority: P1)

A developer inspecting Go code wants source locations for likely constructor/factory functions and common provider or invocation wiring, without requiring a Go toolchain or resolving a dependency graph.

**Why this priority**: Go is already recognized in repository inventory but remains inventory-only, despite common constructor functions and widely used DI frameworks.

**Independent Test**: Scan temporary Go files containing `NewType` functions/calls and Fx/Dig provider wiring. Verify candidate categories, language, paths, lines, comment/literal filtering, Markdown/JSON parity, and deterministic output.

**Acceptance Scenarios**:

1. **Given** Go constructor/factory names or recognized Fx/Dig calls, **When** BrownPants inspects the repository, **Then** it reports source-located `Candidate` signals labeled Go.
2. **Given** matching text only in line/block comments, interpreted strings, rune literals, or raw strings, **When** BrownPants inspects the repository, **Then** those contents produce no candidate signals.
3. **Given** Go files, **When** language coverage is reported, **Then** Go is labeled as lexical candidate analysis.
4. **Given** repeated scans of identical sources, **When** Markdown and JSON are rendered, **Then** records remain equivalent, stable, and correctly located for LF and CRLF.

## Edge Cases

- Go raw string literals use backticks and may span multiple lines.
- Comments and quoted literals can contain pattern-shaped text.
- Constructor naming conventions are heuristic and may match unrelated functions.
- Fx/Dig-style calls may appear without recognizable imports or may be aliased.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST analyze `.go` files for bounded lexical architecture candidates.
- **FR-002**: BrownPants MUST identify `NewType` and common create/build/make/resolve factory function declarations and calls.
- **FR-003**: BrownPants MUST identify common Fx/Dig provider registration, invocation, and container creation calls.
- **FR-004**: Each finding MUST identify Go, repository-relative path, source line, and `Candidate` evidence.
- **FR-005**: Matching MUST ignore line/block comments, interpreted strings, rune literals, and multiline raw strings.
- **FR-006**: Markdown and JSON MUST expose equivalent deterministic records with bounded samples and correct LF/CRLF locations.
- **FR-007**: Documentation MUST state that naming and call matches are lexical candidates and do not prove framework use, resolved dependencies, or runtime behavior.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Go fixtures produce the expected source-located constructor and DI records in both report formats.
- **SC-002**: Pattern-shaped text found only in comments or string forms produces no matches.
- **SC-003**: Repeated Markdown and JSON output is byte-identical, with LF/CRLF locations preserved.
- **SC-004**: Existing C#, VB.NET, C/C++, Python, JavaScript/TypeScript, and JVM analyzer behavior remains covered by passing tests.

## Assumptions

- Go analysis remains dependency-free; BrownPants does not run `go`, compile packages, or execute source.
- `NewType` naming and Fx/Dig call names are navigation cues, not proof of object construction or a valid dependency graph.
- Custom import aliases and arbitrary DI frameworks remain outside this feature's bounded patterns.
