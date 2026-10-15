# Feature Specification: Unique Semantic Evidence

**Feature Branch**: `063-unique-semantic-evidence`
**Created**: 2026-10-15
**Status**: Draft
**Input**: BrownPants semantic evidence for the Bug Lab contained 38 resolved relationships but emitted 36 managed-call findings. Two different constructor calls on the same source line had the same caller, target, and line, so their finding IDs collided and one was discarded.

## User Scenarios & Testing

### User Story 1 - Receive every resolved source relationship (Priority: P1)

A developer or SDD agent needs every compiler-resolved method and constructor relationship represented in the evidence report, including multiple calls on one line, so impact and triage output do not silently omit source facts.

**Why this priority**: Brownfield code often uses compact or generated-style lines with multiple expressions. Missing one silently weakens downstream call tracing and can make completeness counts misleading.

**Independent Test**: Analyze a fixture with two same-target constructions on one line, verify two uniquely identified findings with distinct source columns, and repeat through clue/changed-path focus.

**Acceptance Scenarios**:

1. **AC-001** Given two compiler-resolved relationships share category, path, line, caller, and target but occupy different expression columns, when semantic evidence is emitted, then each relationship appears exactly once with a unique ID, its own expression, and its one-based source column.
2. **AC-002** Given duplicate base IDs occur in a report, when finding IDs are finalized, then deterministic disambiguation preserves all distinct findings and keeps all previously unique finding IDs unchanged.
3. **AC-003** Given same-line relationships are selected by `--clue` or `--changed` with `--semantic`, when focused evidence is produced, then all selected relationships remain present with unique IDs and selection reasons, without a duplicate-key failure.
4. **AC-004** Given semantic evidence has no other omitted source samples and the requested limit exceeds its findings, when completeness is reported, then `totalFindingCount` equals emitted unique findings and `findingsTruncated` is false.

## Edge Cases

- Two identical call expressions appear on one line; source columns still distinguish occurrences.
- A deterministic hash candidate for a disambiguated ID collides with an existing ID; disambiguation continues until unique.
- A caller selects a limit below the number of findings; truncation remains true and omission counts reflect the unique finding set.
- Existing candidate grouping continues to link the final unique IDs without changing its exact-key behavior.

## Requirements

### Functional Requirements

- **FR-001**: Every distinct resolved invocation and constructor represented by Roslyn MUST be retained in evidence output; ID collisions MUST NOT drop findings.
- **FR-002**: Managed-call findings MUST include a one-based source column so multiple expressions on the same line can be reviewed independently.
- **FR-003**: IDs MUST be deterministic and unique within each report. Existing unique IDs MUST remain stable; only colliding findings may receive disambiguated IDs.
- **FR-004**: Focus filtering, repeated-candidate group links, and completeness counts MUST use the finalized unique finding set.
- **FR-005**: Evidence JSON additions MUST remain backward-compatible and keep schema version 1.

## Success Criteria

- **SC-001**: A fixture containing two same-target constructor calls on one line emits both once with unique IDs and distinct one-based columns in full and focused semantic evidence.
- **SC-002**: BrownPants-on-BrownPants semantic evidence emits all 38 resolved Bug Lab relationships; total and emitted counts agree and no truncation is reported when limit is 1000.
- **SC-003**: Existing evidence, focus, grouping, Release build, and full test suites pass with the acceptance hash unchanged.

## Assumptions

- A source column is sufficient to distinguish expression positions without adding a span or token payload to every report.
- Finding IDs are useful references for report correlation; preserving existing unique IDs limits disruption to saved reports and consumers.
- The JSON schema remains version 1 because source column is an optional additive field and only previously-colliding IDs change.
