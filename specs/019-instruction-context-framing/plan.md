# Implementation Plan: Instruction Context Frames

**Branch**: `019-instruction-context-framing` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Add deterministic discovery and reporting of repository instruction sources, then provide a reviewable compact response frame with source references, stale-source detection, and explicit host limitations. Keep inventory and frame validation in the engine-neutral CLI; use adapter guidance for model-assisted drafting and Spec Kit integration.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing CLI and System.Text.Json; no new runtime dependency
**Storage**: Repository files; optional frame output is a new Markdown file
**Testing**: xUnit fixture repositories; CLI Markdown and JSON checks
**Target Platform**: .NET 10 supported platforms
**Project Type**: CLI
**Performance Goals**: Bounded, linear pass over recognized instruction files
**Constraints**: Deterministic, offline, read-only by default; never evaluate instructions; explicit custom sources must remain within repository
**Scale/Scope**: Common agent/project instruction conventions and selected custom Markdown/text files

## Constitution Check

- Evidence First: PASS. Source paths, headings, sizes, digests, heuristic scope, and host limitations are labeled as evidence or estimates.
- Broad, Engine-Neutral Core: PASS. Core source inventory and frame schema do not require a specific SDD engine or model; Spec Kit guidance remains an adapter.
- Deterministic and Reviewable Output: PASS. Stable path ordering, bounded source sizes, deterministic Markdown/JSON, and references to exact sections.
- Safe Inspection by Default: PASS. Read-only content inspection; no code execution, network, external paths, or implicit overwrite.
- Specified and Verified Changes: PASS. Fixture tests, output checks, adapter checks, and package smoke check are listed in tasks.

## Project Structure

```text
src/BrownPants.Cli/
├── Analysis/InstructionSourceAnalyzer.cs
├── Analysis/InstructionContextFrameAnalyzer.cs
├── Output/InstructionSourceReportRenderer.cs
└── Program.cs
tests/BrownPants.Tests/
└── InstructionSourceAnalyzerTests.cs
integrations/speckit/
└── commands/context.md
specs/019-instruction-context-framing/
├── contracts/instruction-context-frame.schema.json
├── data-model.md
├── research.md
└── quickstart.md
```

**Structure Decision**: Add standalone CLI commands and analyzers within the existing CLI project and xUnit suite. Keep parsing/discovery separate from report rendering and keep host-specific lifecycle behavior under integrations.

## Design and Risks

The core inventories and validates source references but does not invent semantic summaries. A model or contributor may draft the frame; BrownPants validates that each point cites known sources, records source identity, enforces safe output creation, and reports budget shortfall or conflicts for review. Token counts are approximate unless a tokenizer is explicitly available. Hosts may continue loading every instruction file, so a frame alone cannot promise savings.

## Complexity Tracking

No constitution violations. No additional runtime project or model dependency is introduced.

## Acceptance Criteria Integrity

The acceptance scenarios and success criteria are frozen in `acceptance-criteria.sha256`. `scripts/acceptance-criteria-hash.ps1` hashes those two sections only, normalizes line endings, fails on drift, and requires an explicit `-Refresh` to establish a reviewed new baseline. Spec Kit guidance verifies the baseline before implementation and after changes; CI verifies the active feature's digest.
