# Feature Specification: Focused Evidence Handoff

**Feature Branch**: `052-focused-evidence-handoff`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Improve BrownPants brownfield output so an SDD agent can receive task-relevant evidence instead of reviewing every repository candidate. Use literal bug clues and changed paths as explicit, auditable focus inputs while preserving the full evidence command for repository discovery.

## User Scenarios & Testing

### User Story 1 - Give an agent task-focused repository evidence (Priority: P1)

A developer preparing a bug-fix task has a few concrete clues and/or changed source paths. They want a small handoff that points to matching source evidence, relevant managed callers, and test-path candidates without hiding what the filter selected or claiming causality.

**Why this priority**: Broad evidence handoffs can make an agent sift through unrelated framework and test candidates, increasing input size and the chance it reports unrelated facts.

**Independent Test**: Run focused evidence against a temporary mixed-language repository with matching and unmatched clues, changed C#/VB paths, direct and transitive callers, unrelated architecture signals, and unrelated test files. Verify stable selection and honest counts in JSON.

**Acceptance Scenarios**:

1. **AC-001** Given `evidence` without focus inputs, when it is run on the same repository and options, then its schema and findings remain unchanged from the current full-repository handoff.
2. **AC-002** Given one or more literal `--clue` values, when focused evidence is requested, then findings are included only when their source path/line or recorded evidence matches a clue under the existing investigation matching rules; every included finding records which focus input selected it.
3. **AC-003** Given one or more repository-relative `--changed` paths and semantic mode, when focused evidence is requested, then resolved direct and transitive caller edges from the existing impact analyzer are included, while unrelated source calls are excluded.
4. **AC-004** Given changed paths without semantic mode, when the command runs, then it still returns explicitly scoped source-local candidates, reports that resolved caller impact was not analyzed, and does not imply the graph is complete.
5. **AC-005** Given both clues and changed paths, when the command selects findings, then the selection rule is explicit and deterministic, matched findings are de-duplicated, and unmatched focus inputs are listed without invented evidence.
6. **AC-006** Given invalid, absolute, escaping, missing, or unsupported focus paths and empty clues, when the command runs, then it returns actionable validation errors without scanning outside the repository.
7. **AC-007** Given a result limit, when focused evidence is emitted, then total matched findings, emitted findings, omitted findings, and focus-match reasons are reported consistently in JSON.

### Edge Cases

- Repeated clues or paths do not duplicate a finding; each selection retains all distinct matching reasons.
- A clue may match only a path, only source evidence, multiple files, or no files.
- A changed path may contain no method declaration; candidate findings remain available and the missing impact mapping is stated.
- The same caller can be reached by multiple changed methods; it is emitted once per distinct relationship with the relevant changed target references.
- Syntax-only and candidate evidence remain distinguished from compiler-resolved calls.
- Focused and full outputs remain deterministic and read-only.

## Requirements

### Functional Requirements

- **FR-001**: The `evidence` command MUST accept repeatable literal clue and changed-path focus inputs while retaining the current unfiltered mode.
- **FR-002**: Clue matching MUST reuse the investigation command's bounded literal matching behavior and return source locations without exposing matched source-line text.
- **FR-003**: Semantic changed-path focus MUST reuse the call-impact analyzer for direct and transitive caller relationships rather than infer graph edges from names.
- **FR-004**: Each focused finding MUST retain its original evidence category/status and add auditable match-reason metadata; focus selection MUST NOT change a candidate into a resolved relationship.
- **FR-005**: The report MUST state all requested focus inputs, unmatched clues/paths, total matches, emitted count, omitted count, and whether semantic impact was analyzed.
- **FR-006**: Invalid focus inputs MUST fail safely and MUST NOT permit paths outside the repository root.
- **FR-007**: Existing unfiltered evidence JSON and command behavior MUST remain backward compatible.
- **FR-008**: The feature MUST add no network dependency and MUST NOT execute inspected repository code or tests.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Existing unfiltered evidence fixtures remain byte-identical for the same inputs and options.
- **SC-002**: Focused fixtures include every expected clue match and direct/transitive impact edge, and exclude unrelated source findings.
- **SC-003**: Empty/unmatched clues and invalid paths are reported or rejected without fabricated findings or root escape.
- **SC-004**: JSON output is deterministic; counts reconcile as total matched = emitted + omitted; duplicate selection reasons do not duplicate findings.
- **SC-005**: On the Brownfield Bug Lab, clue/path-focused handoff emits fewer findings than the full handoff while retaining every answer-key fact that is related to the supplied bug clues or changed method.

## Assumptions

- Literal clue matching follows `investigate` semantics and does not attempt probabilistic ranking.
- Focused evidence is a selection aid; it cannot establish root cause, runtime execution, or test coverage.
- Semantic graph scope is limited to compiler-resolved C# and Visual Basic relationships in loaded project configurations.
- A measured reduction in findings demonstrates smaller output only; provider token savings require a separate model run and usage record.
