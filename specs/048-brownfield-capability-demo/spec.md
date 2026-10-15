# Feature Specification: Measurable Brownfield Capability Demo

**Feature Branch**: `048-brownfield-capability-demo`
**Created**: 2026-10-15
**Status**: Completed
**Input**: Create a small realistic legacy-style application and controlled bug exercise that demonstrates BrownPants value in tricky managed call flows and mixed C# / MFC / NHibernate repositories, measured against an otherwise identical SDD run without BrownPants evidence.

## User Scenarios & Testing

### User Story 1 - Understand impact in an unfamiliar mixed codebase (Priority: P1)

A developer inherits a small order-processing application with overloaded generic C# calls, factory/DI wiring, MFC entry points, and NHibernate metadata. Given a bug report and a seeded regression, the developer wants to identify the correct fix and the related code and tests to review without treating unsupported native or ORM links as proven call edges.

**Independent Test**: Run the same frozen task in isolated baseline and BrownPants-assisted copies, score structured findings against a reviewed answer key, and run identical regression and full demo checks.

**Acceptance Scenarios**:

1. **AC-001** Given the demo repository, when BrownPants inventory and architecture analysis run, then the output identifies the managed projects, factory/DI patterns, MFC and interop candidates, and NHibernate package/mapping/configuration candidates with source paths, and labels lexical/framework signals as candidates.
2. **AC-002** Given a managed C# impact path with generic methods and same-name overloads, when semantic impact analysis runs, then it resolves in-project calls to the correct target declarations and records the relevant callers, paths, and lines; ambiguous, unresolved, or unsupported relationships remain explicit.
3. **AC-003** Given the seeded bug and regression check, when the check runs before agent work, then it fails for the documented behavior; after an agent fix, the same focused and full demo checks pass.
4. **AC-004** Given baseline and assisted agent outputs from identical source, task, prompt, model, host, and validation, when an independent evaluator scores them against the frozen answer key, then the report publishes per-condition precision and recall for actionable impact findings, test candidates, and framework-boundary leads, plus token use and run status when available.
5. **AC-005** Given the paired measured results, when a BrownPants advantage is claimed, then assisted impact-finding recall is at least 20 percentage points higher than baseline with no lower precision; otherwise the report states that the demo did not establish that advantage and the demo is revised before publication.
6. **AC-006** Given MFC and NHibernate artifacts, when the report presents them, then it distinguishes supported source-level candidates from semantic C# call relationships and does not imply that BrownPants builds MFC or proves ORM runtime behavior.

## Requirements

- **FR-001**: Keep the demo self-contained, local, deterministic, and free of required network services or new third-party packages.
- **FR-002**: Include intentionally tricky C# generic and overloaded calls, factory or dependency-injection wiring, a native MFC message path, and NHibernate project/mapping/configuration artifacts, with decoys for precision measurement.
- **FR-003**: Maintain a reviewed answer key for known methods, call sites, tests, registrations, and framework-boundary candidates; do not expose it in either agent prompt.
- **FR-004**: Seed the regression only in isolated run copies; keep the committed demo's default checks green.
- **FR-005**: Give both conditions the same source, bug report, acceptance criteria, implementation prompt, model/host, and test procedure. BrownPants reports and lifecycle evidence are the only planned treatment differences.
- **FR-006**: Record actual outputs, changed paths, review results, test outcomes, precision/recall calculations, and provider token counts when available. Do not infer time or cost savings without measured timing/cost data.
- **FR-007**: If BrownPants misses expected relationships or the minimum advantage threshold, record the result and narrow or redesign the demo instead of changing the answer key after runs.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The managed semantic report resolves every answer-key C# call edge that is supported by the loaded project and reports no decoy call as the same target method.
- **SC-002**: The architecture inventory locates all seeded DI/factory, MFC/interoperability, and NHibernate artifacts and excludes committed decoys from positive candidate findings where current analyzers support discrimination.
- **SC-003**: The injected defect has a reproducible failing pre-run check and both formal fixes receive the same passing focused and full validation.
- **SC-004**: The evaluator calculates precision and recall deterministically from versioned expected IDs and each run's structured findings.
- **SC-005**: Published claims meet AC-005 or explicitly report that no measurable BrownPants advantage was demonstrated.

## Assumptions

- A package-free .NET 10 harness can exercise legacy C# patterns while accompanying .NET Framework project metadata, MFC source, and NHibernate mappings remain analysis fixtures.
- Managed call tracing is the scored semantic capability; MFC and NHibernate are scored as located candidates unless current code proves a stronger relationship.
- One controlled task is a scenario demonstration, not a general benchmark across repositories, models, or teams.
