# Feature Specification: Empty Collection Mapping Candidates

**Feature Branch**: `080-empty-collection-mapping-candidates`

**Created**: 2026-11-06

**Status**: Implemented

**Input**: FullMetalFile review found OCR word and line data discarded by a mapping call that read other properties from the same provider result while passing a newly empty list for words.

## User Scenarios & Testing

### User Story 1 - Find possible dropped collection data (Priority: P1)

As a maintainer reviewing data mappings, I want BrownPants to identify method calls that combine fields from a source object with an empty collection argument, so I can check whether source data was accidentally discarded.

**Independent Test**: Scan generated C# fixtures containing a source property plus an empty collection in one call, meaningful collection mappings, comments/string mentions, bounded samples, and the repaired FullMetalFile OCR mapping.

**Acceptance Scenarios**:

1. **AC-001**: A call with a direct member read from an identifier and an empty collection argument reports a bounded source-located candidate naming the source identifier and empty argument position.
2. **AC-002**: Calls that map a non-empty source collection and text/comments/strings that only mention empty collection patterns do not report candidates.
3. **AC-003**: Candidate counts, ordering, omission totals, source limits, and skipped-file counts are deterministic and explicit.
4. **AC-004**: JSON and Markdown expose this as a heuristic candidate section and state that results require human review.

## Success Criteria

- **SC-001**: Generated fixture tests cover positive, negative, determinism, limits, and reporting behavior.
- **SC-002**: BrownPants solution builds and focused tests pass without warnings.
- **SC-003**: Updated BrownPants scans FullMetalFile and does not flag the repaired OCR layout mapping.
- **SC-004**: SpecKit acceptance digest and evidence ledger validate.

