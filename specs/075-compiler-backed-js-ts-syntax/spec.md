# Feature Specification: Compiler-Backed JavaScript and TypeScript Syntax Checks

**Feature Branch**: `075-compiler-backed-js-ts-syntax`

**Created**: 2026-11-02

**Status**: Implemented

**Input**: User goal: improve BrownPants continuously, use Spec Kit and BrownPants while developing FullMetalFile, and prove improvements against that document-management system.

## User Scenarios & Testing

### User Story 1 - Verify JavaScript and TypeScript syntax with a local compiler (Priority: P1)

A maintainer asks BrownPants to check JavaScript or TypeScript syntax using a compiler already installed in the scanned workspace. BrownPants reports actual parser diagnostics with file locations and does not describe valid JSX, TSX, template literals, or regular expressions as syntax failures.

**Why this priority**: BrownPants currently uses bounded recognizers for JavaScript and TypeScript. On FullMetalFile, they report 11 syntax warnings across nine production files, while the installed TypeScript parser and Node parser accept those files.

**Independent Test**: Run the explicit syntax-check option against a fixture with valid JavaScript/TypeScript and malformed source, then compare the diagnostics with the local compiler parser. No project build or source execution is needed.

**Acceptance Scenarios**:

1. **AC-001** **Given** a workspace with a discoverable local TypeScript compiler and valid JS/TS/JSX/TSX files, **when** the maintainer requests parser verification, **then** BrownPants reports no parser errors for those files.
2. **AC-002** **Given** a file with a genuine parser error, **when** the maintainer requests parser verification, **then** BrownPants reports the compiler's diagnostic code, message, and source location.
3. **AC-003** **Given** a workspace with multiple installed compiler versions, **when** the maintainer requests parser verification, **then** BrownPants uses and identifies the deterministic nearest applicable compiler for each source file.

### User Story 2 - Keep inspection safe and explain unavailable verification (Priority: P1)

A maintainer runs the normal inspection or requests parser verification in a workspace without a local compiler. BrownPants preserves the current dependency-free behavior by default, clearly reports when parser verification did not run, and never downloads packages, runs package scripts, builds projects, or evaluates source files.

**Why this priority**: The scanner must remain useful in mixed-language and offline repositories, while compiler-backed evidence requires executing a local parser library and must be an explicit choice.

**Independent Test**: Run default inspection and the explicit option in an empty/no-compiler fixture; confirm default output is unchanged in behavior and the explicit run records unavailable status without network or project evaluation.

**Acceptance Scenarios**:

1. **AC-004** **Given** any repository, **when** the maintainer runs inspection without the new option, **then** BrownPants does not invoke Node or change the existing bounded-analysis behavior.
2. **AC-005** **Given** no supported local TypeScript compiler is available, **when** parser verification is requested, **then** BrownPants reports that verification is unavailable and retains clearly labeled heuristic evidence.
3. **AC-006** **Given** parser verification is requested, **when** the scan completes, **then** BrownPants has invoked only the local parser API and has not run project source, package scripts, builds, or network operations.

### User Story 3 - Recognize React projects and UI source (Priority: P1)

A maintainer inspecting a React repository sees which package manifests declare React. When parser verification is explicitly enabled, BrownPants also sees source-located React component and hook-call candidates in JSX/TSX code so the maintainer can navigate FullMetalFile's UI without treating syntax as runtime behavior.

**Why this priority**: FullMetalFile is a React/TypeScript business application, so BrownPants needs to understand both its backend and its UI to support the user's end-to-end completion work.

**Independent Test**: Scan a temporary package with a React dependency and TSX component using the deterministic manifest analyzer and parser contract; confirm the project, component, and hook call locations appear as candidates.

**Acceptance Scenarios**:

1. **AC-007** **Given** a package manifest declares React in dependencies, devDependencies, or peerDependencies, **when** BrownPants scans the repository, **then** it reports the React project and manifest path.
2. **AC-008** **Given** parser verification is enabled for a detected React project, **when** BrownPants parses TSX with a function component and hook calls, **then** it reports source-located component and hook-call candidates and labels them as syntax evidence.

## Requirements

### Functional Requirements

- **FR-001**: The default inspection MUST preserve its current no-build, no-network, no-project-code-execution behavior and MUST NOT invoke a JavaScript runtime.
- **FR-002**: An explicit option MUST request parser-only syntax verification for supported JavaScript and TypeScript files using a compiler already present in the scanned workspace.
- **FR-003**: Parser diagnostics MUST include a repository-relative path, compiler diagnostic code, message, and one-based line and column when the compiler provides them.
- **FR-004**: BrownPants MUST identify each parser name/version and its resolved compiler path or invocation used for verification.
- **FR-005**: BrownPants MUST report verification status for each supported source file or a deterministic group of files, distinguishing parser-verified, unavailable, and failed-to-run states from lexical heuristic findings.
- **FR-006**: Compiler discovery and process input MUST be bounded and MUST NOT read files outside the repository root or follow source/configuration paths outside it.
- **FR-007**: Parser verification MUST NOT invoke package-manager commands, package scripts, project builds, type checking, or project configuration evaluation.
- **FR-008**: If the compiler is absent or cannot be safely invoked, BrownPants MUST report the reason and retain its existing heuristic analysis without presenting heuristic warnings as compiler diagnostics.
- **FR-009**: Markdown and JSON output MUST expose the same verification state and source diagnostics in deterministic order.
- **FR-010**: Tests MUST cover valid JSX/TSX, nested template literals, regex literals, genuine malformed syntax, multiple compiler discovery candidates, unavailable compiler behavior, path containment, and default-mode non-invocation.
- **FR-011**: BrownPants MUST detect React projects from declared React package dependencies in bounded `package.json` manifests and report the source manifest path.
- **FR-012**: When parser verification is enabled for React projects, BrownPants MUST report syntax-based React component and hook-call candidates with source locations, without claiming binding or runtime use.

### Key Entities

- **Parser diagnostic**: A source-located syntax error returned by the local compiler parser.
- **Verification state**: The recorded outcome for a source file: parser-verified, unavailable, or failed.
- **Heuristic finding**: Existing bounded-recognizer output, explicitly distinct from compiler parser diagnostics.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All valid files in the representative JS/TS/JSX/TSX fixture produce zero compiler parser diagnostics.
- **SC-002**: Every intentionally malformed fixture is reported with the same compiler diagnostic code and source location as the parser API.
- **SC-003**: The FullMetalFile production client reports zero compiler syntax errors for files accepted by its installed TypeScript parser; its existing default scan remains unchanged.
- **SC-004**: In the no-compiler fixture, verification is reported unavailable, fallback findings remain labeled heuristic, and no network or project build is invoked.
- **SC-005**: Markdown and JSON reports agree on verification states, diagnostic counts, and sorted diagnostic details.
- **SC-006**: FullMetalFile's React package is detected, and parser-verified TSX reports source-located component and hook-call candidates without runtime claims.

## Assumptions

- The existing local TypeScript compiler package is the supported parser provider for JS/TS syntax verification.
- Compiler parser execution is an explicit opt-in because loading a workspace dependency executes that dependency's parser code.
- Parser diagnostics are syntax-only; they do not establish type correctness, module resolution, buildability, runtime behavior, or test coverage.
- Repositories without Node.js or a local TypeScript package continue to use default BrownPants inspection without additional dependencies.
- React component and hook reports are navigation candidates; package declarations and syntax do not prove runtime execution or correct React behavior.
