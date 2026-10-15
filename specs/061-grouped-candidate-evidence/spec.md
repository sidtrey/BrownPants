# Feature Specification: Grouped Candidate Evidence

**Feature Branch**: `061-grouped-candidate-evidence`
**Created**: 2026-10-15
**Status**: Draft
**Input**: The measured BrownPants-assisted comparison found that three of seven extra findings were repeated locations for the same candidate signal. The handoff lists source locations individually, while agents and the reviewed benchmark key often report one signal with several lines.

## User Scenarios & Testing

### User Story 1 - Review repeated candidate locations together (Priority: P1)

A developer or SDD agent reviewing a structured BrownPants report needs repeated architecture/framework signals in the same file grouped together so it can report the signal once with all relevant locations, without losing the original per-location evidence.

**Why this priority**: Repeated framework and architecture candidates are common in brownfield code. Listing every line as a separate finding makes handoffs noisy and can cause duplicate findings in review and QA summaries.

**Independent Test**: Analyze a fixture with the same candidate category and signal at multiple lines in one file, plus distinct signals in the same file and matching signals in another file. Verify only exact category/signal/path matches group, locations stay source-linked, and existing findings remain unchanged.

**Acceptance Scenarios**:

1. **AC-001** **Given** two or more architecture or framework candidate findings share the same category, signal, and path, **When** evidence JSON is produced, **Then** it includes one deterministic repeated-candidate group listing their sampled lines and finding IDs.
2. **AC-002** **Given** candidate findings differ by category, signal, or path, **When** evidence groups are produced, **Then** those findings remain in separate groups and no unrelated location is merged.
3. **AC-003** **Given** a grouped candidate report is emitted, **When** existing clients read `findings`, **Then** per-location rows, IDs, completeness, and schema version remain unchanged.
4. **AC-004** **Given** focused evidence selects findings by clue or changed path, **When** repeated-candidate groups are emitted, **Then** they contain only selected finding IDs and preserve the matching reasons available on those findings.
5. **AC-005** **Given** the brownfield comparison reports repeated source locations as one finding with multiple lines, **When** results are scored, **Then** the evaluator accepts the grouped shape, rejects unrelated lines, and publishes recall, precision, and provider-reported token counts without relaxing the frozen task criteria.

## Edge Cases

- A group contains one sampled member because the finding sample is truncated; it is not labeled a repeated group.
- Several separate candidate signals share one file and line; only exact category/signal/path keys group.
- A same-signal group has locations outside the raw finding sample; report completeness remains incomplete and the group only describes emitted members.
- Resolved managed calls, native-flow relations, and test-path findings are not grouped by this feature.

## Requirements

### Functional Requirements

- **FR-001**: The evidence report MUST expose groups only for emitted `architecture-candidate` and `framework-candidate` findings with at least two source-located rows.
- **FR-002**: Group identity MUST use exact category, signal, and normalized repository-relative path values.
- **FR-003**: Each group MUST list sorted source lines and the existing finding IDs for its sampled member rows.
- **FR-004**: Group ordering and location ordering MUST be deterministic.
- **FR-005**: Groups MUST be derived from the bounded emitted finding set; they MUST NOT increase the number of source locations beyond the existing `--limit` sample.
- **FR-006**: Existing per-location findings, counts, completeness state, focus reasons, and schema version MUST remain unchanged.
- **FR-007**: Comparison instructions and result schema MUST allow one candidate finding to carry multiple exact source lines; the scorer MUST not count valid grouped lines as duplicate false positives or accept unrelated lines.
- **FR-008**: Benchmark results MUST be reported honestly against the frozen answer key and criteria; do not claim quality improvement unless observed precision improves without recall loss.

## Key Entities

- **Repeated candidate group**: A compact roll-up keyed by candidate category, signal, and source path, containing the IDs and source lines of emitted findings with that exact key.
- **Evidence finding**: The existing per-location source-linked item; its shape and identity remain unchanged.

## Success Criteria

- **SC-001**: BrownPants' current bug-lab handoff groups the five repeated candidate sets (C++/CLI bridge, MFC message map, NHibernate session factory, NHibernate session, and NHibernate transaction) with the exact lines present in the per-location findings.
- **SC-002**: Tests prove only matching category/signal/path candidates group, ordering is stable, focused IDs and reasons are retained, and existing report fields/counts are unchanged.
- **SC-003**: The revised matched bug comparison retains all 20 expected facts, reports provider token counts, and has higher assisted precision than the previous 74.1% while recall remains 100%; publish the full result and limitations.
- **SC-004**: The group roll-up and report remain bounded by the existing findings sample and retain schema version 1.

## Assumptions

- Grouping is a navigation aid, not a claim that locations have the same runtime behavior or relevance to a specific bug.
- The original finding rows remain canonical; clients that do not use the additive group field continue to work unchanged.
- The comparison uses the existing fixed bug, acceptance hash, model family, scoring key, and independent post-fix check; only grouping guidance and result serialization are updated.
