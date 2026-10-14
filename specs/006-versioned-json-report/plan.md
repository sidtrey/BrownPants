# Implementation Plan: Versioned JSON Report

**Branch**: `006-versioned-json-report` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/006-versioned-json-report/spec.md`

## Summary

Add an integer `SchemaVersion: 1` field to every successful JSON repository inventory and document the compatibility rule. Keep Markdown output unchanged. The version is independent of package version and repository contents.

## Technical Context

**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: Existing System.Text.Json; no new dependency
**Storage**: None
**Testing**: xUnit CLI/serializer tests
**Target Platform**: .NET 10 supported desktop/server platforms
**Project Type**: CLI
**Performance Goals**: No material cost beyond serializing one integer
**Constraints**: Preserve all existing JSON fields and values; emit the marker only in JSON; use a fixed integer independent of package releases
**Scale/Scope**: One root property and report contract documentation

## Constitution Check

The current constitution requires evidence-first reports, a broad engine-neutral core, deterministic output, safe inspection, and Spec Kit task coverage.

- Evidence first: pass; no analysis behavior or claims change.
- Determinism: pass; the constant field does not depend on repository state or time.
- Engine-neutral JSON: pass; all consumers can inspect the same marker.
- Safe default inspection: pass; no filesystem or MSBuild behavior changes.
- Specified and verified changes: pass; acceptance tests cover JSON cases and Markdown exclusion.

**Post-design gate**: pass; existing fields remain intact and version policy is documented.

## Project Structure

### Documentation (this feature)

```text
specs/006-versioned-json-report/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── report-version.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/BrownPants.Cli/Analysis/RepositoryInventory.cs # add the stable report version
tests/BrownPants.Tests/RepositoryScannerTests.cs    # assert version in JSON scenarios
README.md                                           # explain how consumers use the version
specs/002-cross-ecosystem-inventory/contracts/project-inventory.md # record the JSON field
```

**Structure Decision**: Add the constant to the existing serialized inventory record. Keep CLI formatting and JSON serializer behavior unchanged.

## Complexity Tracking

No new project, dependency, or configuration is required.

