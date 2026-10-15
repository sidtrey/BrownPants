# Feature Specification: Semantic Resolution Coverage

**Feature Branch**: `033-semantic-resolution-coverage`

**Created**: 2026-10-16

**Status**: Draft

**Input**: Idea queue item 10: make syntax-only invocation inventory distinct from semantic target resolution, and make project loading, unresolved calls, and incomplete MSBuild analysis easier to assess.

## User Scenarios & Testing

### User Story 1 - Understand call-resolution evidence (Priority: P1)

A developer reviewing a BrownPants report needs to know which invocation names are syntax samples, which calls were bound to methods, where an in-repository target is declared, and what prevented other calls or projects from being resolved. They need enough coverage information to decide whether to use the report for navigation or investigate project configuration first.

**Why this priority**: Semantic call tracing is useful only when users can distinguish resolved relationships from syntax samples and incomplete project loads. Ambiguous counts can lead developers and QA to miss impacted code or spend time investigating false defects.

**Independent Test**: Inspect temporary C# and Visual Basic projects containing an in-repository call, an external or metadata target, a `nameof` expression, an unresolved call with candidates, a standalone project outside a solution, and a project that fails to load. Verify Markdown and JSON explain project coverage, call counts, target locations, unresolved reasons, and limitations.

**Acceptance Scenarios**:

1. **AC-001** — Given default inspection, when a report lists invocation expressions, then it states that the listed names are syntax-only and does not imply that a target method was resolved. When `--semantic` is selected, the report clearly labels semantic results as compiler-bound method calls for the loaded project configuration.
2. **AC-002** — Given semantic analysis of a repository, when a call resolves to a method declared in a repository source file, then JSON includes its target path and line and Markdown shows the same target location beside the caller and target symbol. Metadata or external targets remain resolved symbols without an invented repository source location.
3. **AC-003** — Given semantic analysis encounters invocation syntax, when the summary is emitted, then the counts distinguish syntax nodes seen, calls analyzed, calls resolved, calls unresolved, and syntax intentionally skipped by policy; `calls analyzed = resolved + unresolved` and `syntax nodes seen = calls analyzed + skipped`.
4. **AC-004** — Given unresolved calls, when semantic results are rendered, then JSON and Markdown report total unresolved counts by Roslyn candidate reason and samples retain candidate symbols when available. A missing or ambiguous target is described as unresolved evidence, not as a confirmed code defect.
5. **AC-005** — Given a repository containing solutions and discovered C# or Visual Basic project files, when semantic inspection runs, then every discovered project is attempted at most once, including projects not listed in a solution. Reports distinguish discovered, loaded, and failed project counts and include bounded load-issue samples with an accurate total.
6. **AC-006** — Given MSBuild workspace failures or compiler errors in loaded projects, when a semantic report is emitted, then it includes the observed failure/error totals, bounded source-located samples where available, and an explicit project-coverage state. It must not describe partial project loading as complete.

### Edge Cases

- A repository has no solutions, several solutions, or a standalone project not referenced by any solution.
- One project appears in more than one solution; semantic results must not double-count its source calls.
- A solution fails to load but one or more of its discovered project files can load directly.
- Invocation syntax resolves to an in-repository method, an external source method, a metadata method, an ambiguous candidate set, or no candidate.
- `nameof(...)` is present in C# and must be excluded from method-call resolution with an explicit skipped count.
- Issue or call samples are truncated by `--limit`; totals remain accurate and omission counts are visible.
- A project loads with compiler errors or warnings; those diagnostics do not by themselves prove a runtime defect.
- C# and Visual Basic projects are both present and are reported under the same .NET semantic pass.

## Requirements

### Functional Requirements

- **FR-001**: Default syntax reports MUST explicitly state that invocation expressions are names found in source and their target methods are not resolved.
- **FR-002**: Semantic reports MUST distinguish syntax nodes seen, calls analyzed, resolved calls, unresolved calls, and calls skipped by an explicit policy, and the reported totals MUST reconcile.
- **FR-003**: Semantic reports MUST include in-repository target paths and lines for resolved source methods in both Markdown and JSON; external and metadata targets MUST remain without repository target paths.
- **FR-004**: Semantic reports MUST count unresolved calls by compiler binding candidate reason and retain bounded candidate-symbol samples without presenting unresolved calls as defects.
- **FR-005**: Semantic loading MUST attempt every discovered C# and Visual Basic project, including standalone projects not included by a discovered solution, while deduplicating projects included by multiple solutions.
- **FR-006**: Semantic reports MUST distinguish discovered, loaded, and failed projects and show the total number of workspace/project-load issues, bounded issue samples, and omitted issue count.
- **FR-007**: Semantic reports MUST count compiler errors in loaded projects and provide bounded source-located diagnostic samples when locations are available. These errors MUST be labeled as compiler diagnostics, not runtime or test failures.
- **FR-008**: Markdown and JSON MUST describe the same semantic totals, target locations, project coverage, issue counts, and unresolved-reason counts.
- **FR-009**: Semantic project loading remains opt-in, deterministic, repository-scoped, and subject to the existing notice that MSBuild imports and design-time targets may execute.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Fixture assertions verify the call-count reconciliation equations for C# and Visual Basic invocations, including skipped `nameof` expressions.
- **SC-002**: A resolved call to repository source includes the same correct relative target path and line in JSON and Markdown; an external or metadata target has no repository target path.
- **SC-003**: A fixture with a solution, a repeated project reference, an unlisted standalone project, and a failed project produces deduplicated and accurate discovered/loaded/failed totals.
- **SC-004**: Unresolved binding reasons, workspace issues, and compiler errors have accurate total counts, bounded samples, and explicit omission counts when the sample limit is exceeded.
- **SC-005**: BrownPants self-inspection and the full existing test suite pass without changing default inspection to load MSBuild projects.

## Assumptions

- Roslyn `IMethodSymbol` binding is the semantic relationship this feature reports; object construction, property access, reflection, dynamic dispatch, and runtime call behavior are outside invocation-call counts unless represented by a method invocation expression.
- An unresolved binding, candidate symbol, MSBuild warning, or compiler error is evidence for review and is not by itself proof of a runtime bug.
- Project and call sample limits affect examples only; totals must describe all analyzed repository projects and source invocations.
- `--semantic` is already an explicit user opt-in to loading MSBuild projects; this feature does not change that execution boundary.
