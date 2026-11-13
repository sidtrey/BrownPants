# Feature Specification: C# No-op Implementation Candidates

**Feature Branch**: `079-csharp-noop-implementation-candidates`

**Created**: 2026-11-06

**Status**: Implemented

**Input**: While using BrownPants to trace FullMetalFile audit reads, a separate source search was required to find PostgreSQL methods that returned empty collections, null, zero, false, or completed tasks without implementing the requested persistence operation.

## User Scenarios & Testing

### User Story 1 - Find suspicious no-op methods in C# repositories (Priority: P1)

As a maintainer improving a brownfield application, I want BrownPants to surface methods whose entire implementation is an obvious placeholder result, so I can inspect those paths before relying on their behavior.

**Independent Test**: Scan a generated C# fixture containing no-op task/collection/default-return methods, a `NotImplementedException`, and neighboring methods with real bodies; inspect structured and Markdown evidence.

**Acceptance Scenarios**:

1. **AC-001**: Given C# methods whose complete body returns `Task.CompletedTask`, an empty collection result, or a constant default result, when BrownPants inspects the repository, then it reports bounded source-located candidates with method signatures and evidence snippets.
2. **AC-002**: Given a C# method that throws `NotImplementedException`, when inspected, then it is reported as an unimplemented-method candidate.
3. **AC-003**: Given normal methods, comments, and string literals that merely mention placeholder patterns, when inspected, then they do not become candidates.
4. **AC-004**: Given more candidates than the configured sample limit, when inspected repeatedly, then counts and retained candidates remain deterministic and truncation is explicit.
5. **AC-005**: Given Markdown or JSON output, when the repository inventory is rendered, then the new candidate report is additive and explicitly says candidates may be intentional and require review.

## Requirements

- **FR-001**: Analyze C# syntax without evaluating or executing repository code.
- **FR-002**: Report only source candidates with path, line, method signature, candidate category, and bounded source evidence.
- **FR-003**: Respect the global sample limit and a per-file read limit; report skipped files and omitted candidates.
- **FR-004**: Do not describe a candidate as a proven defect or infer intended behavior.
- **FR-005**: Keep JSON schema additions backward-compatible and provide a clear Markdown section.

## Success Criteria

- **SC-001**: Generated fixtures detect each targeted no-op pattern while excluding meaningful method bodies and comment/string noise.
- **SC-002**: Sample limits and path ordering are deterministic, and omission counts are accurate.
- **SC-003**: BrownPants solution builds and focused tests pass without warnings.
- **SC-004**: Running the updated scanner on FullMetalFile reports `PostgreSqlUnitOfWork.CreateNotImplemented` as a `NotImplementedException` candidate.
