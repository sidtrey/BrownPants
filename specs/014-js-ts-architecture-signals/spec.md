# Feature Specification: Report JavaScript and TypeScript Factory and DI Signals

**Feature Branch**: `014-js-ts-architecture-signals`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User goal: make BrownPants useful across codebases, including JavaScript and TypeScript repositories that use factory and dependency-injection patterns.

## User Scenarios & Testing

### User Story 1 - Find JavaScript and TypeScript construction wiring (Priority: P1)

A developer inspecting a web or Node.js repository wants source-located leads to common factory and dependency-injection conventions so they can trace how services are created and supplied.

**Independent Test**: Scan temporary JavaScript and TypeScript files with factory conventions and Angular/Inversify DI markers; verify language, category, path, line, Markdown/JSON parity, and filtering of comments and strings.

**Acceptance Scenarios**:

1. **Given** a JavaScript or TypeScript factory or recognized DI convention, **When** BrownPants inspects the repository, **Then** it reports a source-located candidate for the correct language.
2. **Given** the same pattern text appears only in a comment, quoted string, or template literal, **When** BrownPants inspects the repository, **Then** it emits no signal for that text.
3. **Given** JavaScript or TypeScript files, **When** language coverage is reported, **Then** both languages are labeled as lexical candidate analysis.

## Edge Cases

- Line and block comments may contain pattern-shaped text.
- Single-quoted, double-quoted, and multiline template literals may contain pattern-shaped text.
- CRLF and LF inputs preserve declaration line numbers.
- JavaScript and TypeScript findings remain candidates; syntax is not type-checked or semantically resolved.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST recognize JavaScript and TypeScript source extensions, including JSX/TSX and module variants, for lexical architecture candidates.
- **FR-002**: BrownPants MUST report source-located factory class/function/method/invocation naming candidates.
- **FR-003**: BrownPants MUST recognize common DI cues including Angular `@Injectable`, `@Inject`, `inject(...)`, and `useFactory`, plus Inversify-style `@injectable`, `@inject`, `container.bind(...)`, and `container.get(...)`.
- **FR-004**: Each finding MUST identify JavaScript or TypeScript, repository-relative path, line, and `Candidate` evidence.
- **FR-005**: Matching MUST ignore line comments, block comments, quoted strings, and template literal contents.
- **FR-006**: Markdown and JSON MUST expose equivalent records with stable ordering, bounded samples, and correct line numbers for LF and CRLF source.
- **FR-007**: Language coverage and documentation MUST state that findings are lexical candidates and do not establish framework use, resolved dependency relationships, type correctness, or runtime behavior.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Representative JavaScript and TypeScript fixtures produce the expected candidate records in both report formats.
- **SC-002**: Comment-, quoted-string-, and template-only fixtures produce no matching candidate signals.
- **SC-003**: Existing Python, C#, VB.NET, and native findings continue to pass unchanged.
- **SC-004**: Repeated scans of identical fixtures produce byte-identical Markdown and JSON.

## Assumptions

- BrownPants uses a dependency-free lexical pass; no JavaScript runtime, TypeScript compiler, or package installation is required.
- Framework markers are conventions that guide inspection, not proof that a framework is configured or active.
