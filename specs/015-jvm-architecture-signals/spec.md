# Feature Specification: Report JVM Factory and Dependency-Injection Signals

**Feature Branch**: `015-jvm-architecture-signals`

**Created**: 2026-10-14

**Status**: Draft

**Input**: BrownPants should provide useful, source-located architecture evidence across codebases, including JVM repositories that use factories and dependency injection.

## User Scenarios & Testing

### User Story 1 - Find JVM construction and wiring conventions (Priority: P1)

A developer inspecting Java or Kotlin code wants bounded source locations for likely factory and dependency-injection declarations and calls, without requiring a JVM build or resolving a framework graph.

**Why this priority**: Java/Kotlin are already inventoried as JVM languages but otherwise provide no source-level architecture evidence.

**Independent Test**: Scan temporary `.java`, `.kt`, and `.kts` files with factory, Spring, Jakarta Inject, and Dagger/Guice cues. Verify candidates, language labels, paths, lines, noise filtering, Markdown/JSON parity, and deterministic output.

**Acceptance Scenarios**:

1. **Given** Java or Kotlin factory naming and common DI declarations, **When** BrownPants inspects the repository, **Then** it reports source-located `Candidate` signals with the correct language.
2. **Given** matching text only inside line/block comments, Java text blocks, Kotlin raw strings, or quoted literals, **When** BrownPants inspects the repository, **Then** those contents produce no candidate signals.
3. **Given** JVM source files, **When** language coverage is reported, **Then** Java and Kotlin are labeled as lexical candidate analysis.
4. **Given** the same source on repeated scans, **When** Markdown and JSON are rendered, **Then** record ordering and line locations remain stable and equivalent.

## Edge Cases

- Java text blocks and Kotlin triple-quoted raw strings span multiple lines.
- Kotlin block comments may nest; Java comments do not.
- LF and CRLF line endings preserve source locations.
- Framework annotations are cues, not proof that the framework is active or that a binding resolves.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST analyze `.java`, `.kt`, and `.kts` files for bounded lexical architecture candidates.
- **FR-002**: BrownPants MUST identify factory type, method, and invocation naming candidates.
- **FR-003**: BrownPants MUST recognize common Spring, Jakarta Inject/CDI, Dagger, and Guice cues, including bean/provider/bind declarations, injector/component creation or resolution calls, and injection annotations.
- **FR-004**: Each finding MUST identify Java or Kotlin, repository-relative path, source line, and `Candidate` evidence.
- **FR-005**: Matching MUST ignore comments and quoted literals, including multiline Java text blocks and Kotlin raw strings, while respecting Kotlin nested block comments.
- **FR-006**: Markdown and JSON MUST expose equivalent candidate records with deterministic order and bounded samples.
- **FR-007**: Language coverage and documentation MUST describe the JVM findings as lexical candidates that do not prove framework use, resolved dependency relationships, type correctness, or runtime behavior.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Java, Kotlin, and Kotlin script fixtures produce the expected source-located records in both report formats.
- **SC-002**: Pattern-shaped text only in comments or string forms produces no matching records.
- **SC-003**: Repeated scans produce byte-identical Markdown and JSON; LF and CRLF fixtures preserve line locations.
- **SC-004**: Existing C#, VB.NET, C/C++, Python, and JavaScript/TypeScript behavior remains covered by passing tests.

## Assumptions

- The JVM analyzer remains dependency-free and does not invoke javac, kotlinc, Gradle, Maven, or project code.
- Framework markers and naming conventions are navigation leads, not semantic findings.
- JVM build and dependency relationships remain outside this lexical feature.
