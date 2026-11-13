# Feature Specification: JavaScript and TypeScript Source Evidence

- **Feature Branch**: `072-ts-source-evidence`
- **Created**: 2026-10-16
- **Status**: Implementation complete; release verification pending
**Input**: User goal: finish a production release with useful JavaScript and TypeScript support, based on the observed gap during a public repository bounty run.

## User Scenarios & Testing

### User Story 1 - Understand JS/TS declarations and call paths (Priority: P1)

A developer entering a JavaScript or TypeScript codebase needs source-located declarations, module relationships, and bounded call evidence to understand affected code before changing it.

**Independent Test**: Scan a temporary mixed JS/TS workspace and assert exact declaration, import/export, call, resolution status, path and line records in JSON and Markdown, including unresolved relationships and output limits.

**Acceptance Scenarios**:

1. **AC-001** Given JS, TS, JSX and TSX fixtures, when inspected, then supported declarations, imports, exports and calls are reported with exact repository-relative source locations and stable ordering.
2. **AC-002** Given relative imports, package workspace declarations, and configured `tsconfig` path aliases, when targets exist, then BrownPants resolves only unambiguous in-repository targets and labels the relationship as syntax-resolved rather than compiler-bound.
3. **AC-003** Given overload-like declarations, generic functions, barrel exports and JSX/TSX calls, when inspected, then records remain bounded, do not multiply a call across same-name declarations, and retain the declared target ambiguity where syntax cannot choose an overload.
4. **AC-004** Given dynamic imports, missing targets, unsupported aliases, or runtime dispatch, when inspected, then BrownPants emits bounded unresolved evidence with a reason and does not fabricate a target.
5. **AC-005** Given identical repository contents and sample limits, when rendered as JSON and Markdown repeatedly, then records and counts are deterministic and each format identifies the analysis evidence level and limits.

## Edge Cases

- Comments, strings, template literals, regex literals, and JSX text must not create declarations or calls.
- CRLF/LF, nested folders, `index` modules, extensionless imports, `package.json` workspaces, path alias wildcards, missing manifests, malformed JSON, and cyclic re-exports must have deterministic outcomes.
- Dynamic imports, computed module paths, package exports conditions, type-only imports, same-name overloads, and unresolved external packages must not be described as resolved source calls.
- File counts, declarations, relationships, diagnostics, and samples must have explicit limits; truncation must be visible.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST analyze `.js`, `.jsx`, `.mjs`, `.cjs`, `.ts`, `.tsx`, `.mts`, and `.cts` files during default offline inspection without executing repository code, installing packages, or accessing the network.
- **FR-002**: Reports MUST provide source-located function, method, class, and variable-bound callable declaration candidates, plus import/export records and call-site candidates.
- **FR-003**: Reports MUST resolve relative module targets, declared workspace package boundaries, and unambiguous `tsconfig` `baseUrl`/`paths` aliases only when configuration inputs are valid and targets are within the scan root.
- **FR-004**: A relationship MUST distinguish `syntax-resolved`, `ambiguous`, and `unresolved` evidence; a same-file call can resolve to a declaration only when one compatible syntactic target remains. No record may claim TypeScript type checking, overload binding, or runtime dispatch.
- **FR-005**: Dynamic/computed imports and calls, unresolved aliases/targets, malformed configuration, and unsupported resolution cases MUST include a stable reason and bounded source location.
- **FR-006**: JSON and Markdown MUST expose equivalent totals, bounded deterministic samples, source lines, analysis level, omissions, and evidence limits without changing the top-level inventory schema version.
- **FR-007**: Existing factory and DI candidate signals from feature 014 MUST remain present and regression-tested alongside the new source evidence.
- **FR-008**: Documentation MUST state supported resolution inputs and that syntax resolution does not prove type correctness, package installation, runtime loading, or execution.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Fixture expectations cover all eight supported extensions, declaration/import/export/call records, and exact paths and lines in both report formats.
- **SC-002**: Relative imports, barrel exports, workspace dependencies, wildcard aliases, missing targets, dynamic imports, generics, overload-like declarations, and JSX/TSX produce the explicitly expected resolved, ambiguous, or unresolved status.
- **SC-003**: Noise fixtures containing matching text only in comments, strings, templates, regex literals, and JSX text produce no false source records.
- **SC-004**: Repeated scans are byte-identical; every configured sample cap is respected and every omitted count is accurate.
- **SC-005**: The complete existing test suite, Release build, required adapter checks, packed-tool smoke test, and a clean local tool install pass for the release candidate.

## Assumptions

- No runtime, package manager, or TypeScript compiler is invoked. Parsing and module resolution are local, bounded, and dependency-free; evidence that requires compiler or runtime semantics remains explicitly unresolved.
- `package.json` workspace forms supported are explicit package paths and glob patterns with `*` segments; `tsconfig` extends chains are followed only when every target stays within the repository and parses successfully. Package `exports` condition selection is not guessed.
- Existing JSON inventory schema remains version 1 because this change adds fields only.
