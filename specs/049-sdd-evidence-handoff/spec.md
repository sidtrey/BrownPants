# Feature Specification: SDD Evidence Handoff

**Feature Branch**: `049-sdd-evidence-handoff`
**Created**: 2026-10-15
**Status**: Completed
**Input**: Improve BrownPants-assisted brownfield tasks after the measured comparison scored 30% recall and 66.7% precision, below the vanilla baseline's 25% recall and 71.4% precision. Reduce missed findings and false positives by giving SDD agents concise, structured, source-located BrownPants evidence.

## User Scenarios & Testing

### User Story 1 - Use BrownPants evidence in an SDD task (Priority: P1)

A developer or agent receives a bug report in an unfamiliar repository. They need one machine-readable evidence handoff that separates semantically resolved managed calls from candidate architecture/framework signals, identifies test locations, and preserves evidence limits so findings can be reported accurately.

**Independent Test**: Generate the evidence handoff for the brownfield bug lab and compare its findings to the existing frozen answer key and decoys. Verify the output against the published schema and source locations.

**Acceptance Scenarios**:

1. **AC-001** Given a repository, when evidence is requested in JSON, then one versioned report contains stable, source-located managed calls, architecture candidates, framework candidates, test candidates, project/language context, and explicit analysis limitations.
2. **AC-002** Given semantic analysis is available, when a call target is resolved, then it is labeled `resolved` with caller and target signatures; unresolved or unsupported relationships are not promoted to resolved findings.
3. **AC-003** Given lexical or pattern-based evidence, when it is included, then it is labeled `candidate` and retains its original signal, path, line, and evidence text.
4. **AC-004** Given the fixture contains decoys and irrelevant signals, when findings are emitted, then identities are deterministic and duplicates are removed without suppressing distinct overloads or source locations.
5. **AC-005** Given an SDD agent uses the handoff, when the frozen comparison is repeated under the existing protocol, then BrownPants-assisted actionable recall is at least 20 percentage points above the recorded vanilla baseline (45% minimum) and precision is at least the baseline's 71.4%; publish counts, token use, and limitations. Do not claim an advantage unless both thresholds pass.

## Requirements

- **FR-001**: Produce a compact, deterministic, versioned JSON evidence handoff from supported BrownPants analysis.
- **FR-002**: Preserve the distinction between resolved managed call edges and candidate pattern/framework evidence.
- **FR-003**: Include repository-relative source paths, line numbers where available, and enough signature/evidence detail to verify findings.
- **FR-004**: Include discovered test paths and explain when semantic analysis was not run or was incomplete.
- **FR-005**: Do not incorporate benchmark answer keys, expected IDs, or task-specific heuristics into the evidence generator.
- **FR-006**: Keep existing inspect, impact, and investigation outputs compatible.
- **FR-007**: Update the bug-lab instructions so agents consume the structured handoff and retain its evidence labels and limits in `result.json`.
- **FR-008**: Rerun the existing fixed comparison protocol and publish the actual score. If the frozen threshold fails, report the failure and do not advertise superiority.

## Success Criteria

- **SC-001**: The generated report conforms to its published schema and serializes deterministically for identical repository contents and options.
- **SC-002**: On the fixture, all four known managed call edges and all five known factory/DI findings are represented with correct identity and status; all ten framework leads and the regression test location are source-located when supported by analysis.
- **SC-003**: The two frozen decoys are not emitted as supported positive findings, and no candidate is labeled resolved.
- **SC-004**: The repeated baseline/assisted experiment meets AC-005 or is published as not meeting the threshold, with no unsupported claim.

## Assumptions

- Existing Roslyn and architecture analyzers remain the evidence sources; this feature improves their handoff to agents rather than claiming new language-analysis capability.
- Test paths and framework signals remain candidates unless the analyzer has stronger evidence.
- A single fixed fixture can diagnose this workflow failure but cannot establish general performance across repositories or models.
