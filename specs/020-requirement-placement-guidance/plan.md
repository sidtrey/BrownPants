# Implementation Plan: Requirement Placement Guidance

**Branch**: `020-requirement-placement-guidance` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Provide a deterministic placement advisor that maps a proposed rule plus explicit scope, durability, and purpose inputs to constitution, feature spec, agent/contributor guidance, split, or clarification. Explain the rubric and input evidence; do not edit project artifacts or rely on an LLM.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing CLI and System.Text.Json; no new runtime dependency
**Storage**: None; output only
**Testing**: xUnit fixtures and CLI Markdown/JSON checks
**Target Platform**: .NET 10 supported platforms
**Project Type**: CLI
**Performance Goals**: Constant time after input validation
**Constraints**: Deterministic, offline, read-only, language and SDD engine neutral
**Scale/Scope**: One proposed rule per invocation; explicit rubric covers three destinations and split/clarification cases

## Constitution Check

- Evidence First: PASS. Rationale cites supplied scope, durability, and purpose; it does not claim to infer intent from wording.
- Broad, Engine-Neutral Core: PASS. Destinations and decision rubric are tool-independent concepts; adapters only provide workflow entry points.
- Deterministic and Reviewable Output: PASS. Identical input yields identical Markdown/JSON with the chosen rule and rationale.
- Safe Inspection by Default: PASS. Classification does not read or modify repository files, execute content, or access the network.
- Specified and Verified Changes: PASS. Decision table and CLI contract receive fixture-based tests.

## Project Structure

```text
src/BrownPants.Cli/
├── Analysis/PlacementGuidanceAnalyzer.cs
├── Output/PlacementGuidanceRenderer.cs
└── Program.cs
tests/BrownPants.Tests/
└── PlacementGuidanceTests.cs
integrations/speckit/commands/context.md
specs/020-requirement-placement-guidance/
├── contracts/placement-guidance.schema.json
├── data-model.md
├── quickstart.md
└── tasks.md
```

**Structure Decision**: Add an independent analyzer and report renderer in the existing CLI; keep the rubric independent of Spec Kit and document how the Spec Kit adapter invokes it in `integrations/speckit/commands/context.md`.

## Decision Model and Risks

The CLI requires explicit `scope`, `durability`, and `purpose` values, rather than guessing from keywords. The analyzer applies a documented decision table and returns a clarification or split result when values are incomplete or conflict. This is deliberately a transparent decision aid; an LLM or contributor can misstate inputs, so reports repeat them and the contributor remains responsible for review.

## Acceptance Criteria Integrity

The acceptance scenarios and success criteria are frozen in `acceptance-criteria.sha256`. Verify before implementation and completion with `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/020-requirement-placement-guidance/spec.md`. Refresh only after review of an intentional criteria change.
