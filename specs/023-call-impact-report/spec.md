# Feature Specification: Call-Trace Change Impact Report

**Feature Branch**: `023-call-impact-report`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User direction: Use call tracing to show QA where changed code may have impact, and give developers or agents useful impact and complexity evidence for decisions.

## User Scenarios & Testing

### User Story 1 - Assess change impact before implementation and QA (Priority: P1)

A developer or QA reviewer names changed source files and wants to see which resolved callers, dependent areas, and test candidates may be affected before deciding what to inspect or rerun.

**Why this priority**: Knowing the likely change radius helps teams plan review and regression work in legacy systems where call relationships are difficult to follow.

**Independent Test**: Scan a temporary multi-project .NET fixture with a changed method, direct and transitive callers, and test callers; verify the impact chain, test-candidate labels, and evidence limits.

**Acceptance Scenarios**:

1. **AC-001** — **Given** a changed C# or VB.NET source file with methods called directly and transitively from other loaded projects, **When** the impact report is generated, **Then** it lists each resolved caller chain with source path, call-site line, and caller depth.
2. **AC-002** — **Given** an affected call site in a likely test path, **When** the impact report is generated, **Then** it labels the path as a test candidate without claiming that the test covers or validates the change.
3. **AC-003** — **Given** a changed path that cannot be mapped to loaded source methods, a project-load issue, unresolved calls, or a truncated call graph, **When** the report is generated, **Then** it identifies the limitation and marks impact completeness accordingly instead of treating missing callers as proof of no impact.
4. **AC-004** — **Given** a resolved impact graph, **When** developers review complexity evidence, **Then** the report gives counts for direct and transitive callers, affected files and projects, test candidates, and maximum observed caller depth without presenting an effort or story-point estimate.
5. **AC-005** — **Given** unchanged repository contents and inputs, **When** Markdown and JSON reports are generated repeatedly, **Then** their impact findings and ordering are deterministic and equivalent.

## Edge Cases

- A changed file contains multiple overloads or methods with no callers.
- A changed path is outside the repository, does not exist, is ignored, or has an unsupported language.
- A call graph contains recursion or multiple paths to the same caller.
- Some projects load while others fail, or call samples exceed the configured limit.
- Native MFC/C++ and managed/native boundary calls are not semantically resolved by the current Roslyn workspace.

## Requirements

### Functional Requirements

- **FR-001**: The impact command MUST accept one or more repository-relative changed source paths and use resolved call relationships from loaded C# and Visual Basic projects to find direct and transitive callers.
- **FR-002**: Each reported impact MUST include the changed method, affected caller, invocation path and line, caller depth, and test-candidate status when available.
- **FR-003**: The report MUST summarize direct and transitive callers, affected files and projects, test candidates, maximum observed caller depth, and whether analysis is complete.
- **FR-004**: Failed project loads, unresolved calls, unsupported paths, and sampled call graphs MUST be reported as limitations; absent edges MUST NOT be represented as proof of no impact.
- **FR-005**: The report MUST NOT claim that a candidate test covers the change or convert graph measurements into deterministic effort estimates.
- **FR-006**: Markdown and versioned JSON output MUST contain equivalent findings with stable ordering.
- **FR-007**: The command MUST be explicitly opt-in because project loading can evaluate MSBuild imports and run design-time targets.
- **FR-008**: Native C++/MFC and managed/native relationships MUST remain identified as unresolved boundaries unless a supported semantic relationship is observed.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A fixture with direct and transitive callers reports every resolved caller once at its shortest discovered depth with a source path and call-site line.
- **SC-002**: Test-path callers appear as test candidates, and output makes no claim that candidate paths prove regression coverage.
- **SC-003**: Fixtures for missing declarations, partial project loads, unsupported inputs, and truncated samples produce explicit incomplete-analysis status and diagnostics.
- **SC-004**: Two runs over the same fixture produce equivalent ordered findings in Markdown and JSON.
- **SC-005**: Complexity evidence reports graph counts and depth, and contains no time, story-point, or effort forecast.

## Assumptions

- Users can provide changed source paths from their review or version-control diff; the command does not infer changed lines or edit files.
- Initial resolved call tracing covers C# and Visual Basic projects loadable by MSBuildWorkspace. Other languages, native C++, and runtime/dynamic relationships remain outside semantic call-graph claims.
- Impact counts describe observed resolved call relationships and are evidence for review planning, not proof of complete repository impact.
