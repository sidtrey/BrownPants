# Implementation Plan: Pluggable SDD Discovery

**Branch**: `005-pluggable-sdd-discovery` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/005-pluggable-sdd-discovery/spec.md`

## Summary

Add optional declarative custom SDD workflow discovery through `.brownpants/sdd-workflows.json`. Validate names and portable repository-relative roots, then derive counts and bounded samples exclusively from the scanner's existing file inventory. Surface deterministic configuration diagnostics in JSON and Markdown while preserving built-in Spec Kit and OpenSpec detection.

## Technical Context

**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: Existing System.Text.Json and .NET base libraries; no new package
**Storage**: Repository-local JSON configuration; no persistent state
**Testing**: xUnit fixture-based tests; existing PowerShell adapter tests
**Target Platform**: .NET 10 supported desktop/server platforms
**Project Type**: CLI
**Performance Goals**: Configuration parsing and validation remain bounded and small relative to repository enumeration; cap configuration input at 1 MiB
**Constraints**: Deterministic output; no external paths, execution, network, or plugin loading; built-in inventory unchanged when configuration is absent
**Scale/Scope**: One optional declaration file, at most 100 workflow definitions, repository-local directory roots

## Constitution Check

The checked-in `.specify/memory/constitution.md` still contains template placeholders and does not define project gates. Apply the actionable repository guidance in [AGENTS.md](../../AGENTS.md) and the feature requirements:

- Deterministic, repository-relative results: pass; custom inventory derives only from the scanner's already enumerated files.
- Avoid unsupported relationship claims: pass; roots classify artifact paths and do not parse engine semantics.
- Preserve language-agnostic core and no required external services: pass.
- Add fixture tests for inventory behavior: pass; tests will cover valid definitions, diagnostics, safety, output, and repeatability.
- No added dependency or separate project: pass.

**Post-design gate**: pass, subject to tests covering malformed definitions, hostile roots, duplicate names, built-in compatibility, and deterministic output.

## Project Structure

### Documentation (this feature)

```text
specs/005-pluggable-sdd-discovery/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── custom-workflow-config.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/BrownPants.Cli/Analysis/
├── RepositoryScanner.cs          # pass root and existing file list to workflow analyzer
├── RepositoryInventory.cs        # expose configuration diagnostics
└── SddWorkflowAnalyzer.cs        # built-in and custom discovery, validation
src/BrownPants.Cli/Output/MarkdownReport.cs # render custom rows and diagnostics
tests/BrownPants.Tests/RepositoryScannerTests.cs # temporary repository integration tests
README.md                                     # document registration format
```

**Structure Decision**: Extend the existing analysis and report pipeline. Keep custom configuration loading and validation in the SDD workflow analysis boundary; no new assembly or external plugin system.

## Complexity Tracking

No constitution violations or additional projects are required.
