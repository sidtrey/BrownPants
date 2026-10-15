# Feature Specification: Valid Legacy Semantic Fixture

**Feature Branch**: `062-valid-legacy-semantic-fixture`
**Created**: 2026-10-15
**Status**: Draft
**Input**: BrownPants semantic self-inspection found that its NHibernate Bug Lab project was missing the C# build targets import. The fixture generated 21 compiler errors and four unresolved NHibernate calls, making it difficult to distinguish project setup problems from analysis limits.

## User Scenarios & Testing

### User Story 1 - Trust the legacy-project evidence (Priority: P1)

A developer reviewing BrownPants' .NET Framework example needs semantic output from a structurally valid legacy project so workspace and compiler diagnostics reflect genuine source or dependency issues.

**Why this priority**: BrownPants uses this repository as its own brownfield validation case. Fixture defects can make the tool appear less capable or obscure actual analysis gaps.

**Independent Test**: Run semantic inspection against the example and verify the legacy project loads without a missing-target workspace issue, retains its .NET Framework target and NHibernate dependency, and reports call resolution and remaining limitations accurately.

**Acceptance Scenarios**:

1. **AC-001** Given the legacy Framework project is inspected semantically, when MSBuild loads it, then it has the required C# targets and no workspace issue caused by a missing `Compile` target.
2. **AC-002** Given the project is analyzed, when Roslyn reports compiler diagnostics and invocation relationships, then project-load failures are distinguished from compiler errors and each NHibernate call is either resolved to its actual target or explicitly reported unresolved with the relevant dependency limitation.
3. **AC-003** Given the fixture is updated, when the Bug Lab's documented regression command and BrownPants semantic inspection run, then the managed fixture behavior is unchanged and report completeness accurately reflects any unavailable legacy build prerequisites.

## Edge Cases

- A host lacks .NET Framework targeting assemblies or legacy project tooling; reports must preserve incomplete coverage rather than imply successful evaluation.
- A NuGet audit or restore fails; the existing dependency version must not be silently upgraded or represented as resolved.
- The example remains analysis-focused and must not require building native MFC/C++/CLI components on every platform.

## Requirements

### Functional Requirements

- **FR-001**: The legacy Framework project MUST declare the C# build targets required for classic project evaluation.
- **FR-002**: The fixture MUST retain its .NET Framework 4.7.2 target and its explicitly declared NHibernate dependency version unless a separately reviewed security finding requires a change.
- **FR-003**: Semantic reports MUST distinguish project-load failures, compiler diagnostics, unresolved calls, and resolved call targets; no unresolved external call may be presented as resolved without compiler evidence.
- **FR-004**: The fixture MUST remain suitable for default offline inventory and must not require native tools for the managed regression check.
- **FR-005**: Documentation and verification MUST state environment prerequisites and observed limits for legacy semantic loading.

## Success Criteria

- **SC-001**: Semantic inspection on a supported Windows environment reports the legacy Framework project loaded with no missing-`Compile`-target workspace issue.
- **SC-002**: The semantic report preserves source-located compiler diagnostics and unresolved-call counts, and does not claim any NHibernate target resolved without compiler evidence.
- **SC-003**: Existing managed Bug Lab checks and the full BrownPants test suite pass, and semantic coverage is not reported complete when required legacy prerequisites are unavailable.

## Assumptions

- The fixture should remain a credible classic .NET Framework example rather than being converted to an SDK-style project solely to satisfy the analyzer.
- Existing dependency versions are part of the scenario; package updates require current advisory and compatibility review.
- Semantic mode is opt-in and may evaluate imported build targets, consistent with repository guidance.
