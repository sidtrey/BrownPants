# Feature Specification: Report MFC Class Inheritance Signals

**Feature Branch**: `011-mfc-class-hierarchy`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User goal: make BrownPants especially useful for mixed C# and MFC applications while preserving support for other repositories.

## User Scenarios & Testing

### User Story 1 - Find MFC application and UI class hierarchies (Priority: P1)

A developer inspecting a native C++ repository wants to find classes that inherit from common MFC framework types so they can locate the application's windows, views, documents, dialogs, and command targets.

**Why this priority**: Existing reports identify MFC project settings, runtime macros, object factories, and message maps, but omit the class inheritance that anchors many MFC applications.

**Independent Test**: Scan a temporary repository with single-line and multiline MFC derived class declarations and verify source-located candidate signals in Markdown and JSON.

**Acceptance Scenarios**:

1. **Given** a class or struct declaration deriving from a recognized MFC base type, **When** BrownPants inspects the repository, **Then** it reports an `MFC class inheritance` candidate with its file and declaration line.
2. **Given** an MFC base type appears only in a comment or string, **When** BrownPants inspects the repository, **Then** it does not report an inheritance signal for that text.
3. **Given** a class inherits from a non-MFC base type, **When** BrownPants inspects the repository, **Then** it does not report an MFC inheritance signal.

## Edge Cases

- The inheritance list spans multiple lines.
- Access specifiers include `public`, `protected`, or `private`.
- A common MFC base appears in a comment, ordinary string, or C++ raw string.
- Existing MFC macro and COM candidate counts and path ordering remain stable except for the new signals.
- The evidence is lexical and does not prove that the class is built, instantiated, or active at runtime.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST report source-located `MFC class inheritance` candidates for class or struct declarations deriving from recognized MFC framework base types.
- **FR-002**: Each signal MUST identify `C++` as the language, the repository-relative source path, the declaration line, and `Candidate` as its evidence kind.
- **FR-003**: Inheritance matching MUST ignore comments and string literals using the existing native source filtering behavior.
- **FR-004**: BrownPants MUST recognize declarations whose class and MFC base appear on different lines.
- **FR-005**: Markdown and JSON MUST expose the same candidate signals without changing existing project or architecture signal semantics.
- **FR-006**: Reports and documentation MUST state that this is lexical candidate evidence and does not establish a resolved class hierarchy or runtime behavior.

## Key Entities

- **MFC inheritance candidate**: A source-located lexical match between a C++ class or struct declaration and a recognized MFC base class.
- **Architecture pattern signal**: The existing report entity containing category, evidence kind, language, path, line, and matched source evidence.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All representative single-line and multiline MFC inheritance fixtures produce the expected candidate category and source location in JSON and Markdown.
- **SC-002**: No fixture containing only comments, strings, raw strings, or non-MFC base classes produces an MFC inheritance candidate.
- **SC-003**: Existing MFC macro, COM, C++/CLI, C#, and VB.NET candidate fixtures continue to pass unchanged.
- **SC-004**: Repeated scans of identical fixtures produce byte-identical reports.

## Assumptions

- Candidate base names are a bounded list of commonly used MFC application and UI classes.
- BrownPants continues to treat native C++ analysis as lexical evidence; a full C++ parser or semantic inheritance resolver is outside this feature.
