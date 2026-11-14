# Feature Specification: Indirect Placeholder Accessors

**Feature Branch**: `081-indirect-placeholder-accessors`

**Created**: 2026-11-06

**Status**: Implemented

## User Scenarios & Testing

### User Story 1 - Find accessors that delegate to unimplemented helpers (Priority: P1)

As a maintainer reviewing a repository inventory, I want BrownPants to identify properties and methods that delegate directly to a same-type helper whose entire body throws or returns a newly constructed `NotImplementedException`, so I can see the API surface that fails when accessed rather than only the helper itself.

**Independent Test**: Scan temporary C# fixtures containing expression-bodied and accessor-bodied property wrappers, a direct method wrapper, direct throwing helpers, unrelated calls, non-throwing helpers, and comments/string text. Verify deterministic source-located candidates and bounded reporting.

**Acceptance Scenarios**:

1. **AC-001**: A property getter or method whose sole expression invokes a same-type helper consisting only of a direct throw or return of a newly constructed `NotImplementedException` is reported as an indirect placeholder candidate with the caller signature and helper name.
2. **AC-002**: A helper with any other behavior, a call to a different type, a multi-step call chain, or text/comments mentioning the pattern is not reported as an indirect candidate.
3. **AC-003**: Candidate ordering, limits, omission counts, and path ordering are deterministic and remain within existing source read bounds.
4. **AC-004**: JSON and Markdown distinguish indirect candidates from direct placeholder bodies and describe them as syntax-only review evidence.

## Requirements

- **FR-001**: Candidate detection MUST use C# syntax and MUST NOT execute or evaluate repository code.
- **FR-002**: Detection MUST be limited to one-hop calls to a same-type helper whose complete body is a direct throw or return expression constructing `NotImplementedException`.
- **FR-003**: Reported evidence MUST include the caller's source location and signature and the delegated helper name.
- **FR-004**: Existing direct placeholder detection and JSON fields MUST remain compatible.
- **FR-005**: Per-file read limits, global sample limits, deterministic ordering, and skipped-file counts MUST be preserved.
- **FR-006**: Reports MUST label results as candidates and MUST NOT claim runtime dispatch or proven failure paths.

## Success Criteria

- **SC-001**: Generated tests detect supported property and method wrappers and exclude unrelated, multi-step, non-throwing, comment, and string cases.
- **SC-002**: BrownPants builds without warnings and its full unit suite passes.
- **SC-003**: A parser-backed FullMetalFile scan reports `Templates`, `FieldDefinitions`, `TemplateRelationships`, and `DocumentVersions` as indirect placeholder candidates on `PostgreSqlUnitOfWork` and does not classify that class's constructor-initialized `IndexingSessions` property as a placeholder.
- **SC-004**: Markdown and JSON evidence agree and the Spec Kit acceptance digest and evidence ledger validate.

## Assumptions

- Same-type, one-hop delegation to a helper whose only operation throws or returns a new `NotImplementedException` is strong enough to report as a candidate without claiming semantic binding.
- Resolving inherited helpers, virtual dispatch, or multi-hop call chains remains outside scope.
