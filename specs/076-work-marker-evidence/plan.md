# Implementation Plan: Source Work Marker Evidence

**Branch**: `076-work-marker-evidence` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

## Summary

Add a deterministic source-only scanner for TODO, FIXME, HACK, and XXX markers. Report marker type and source location without line text. Bound per-file reads, aggregate bytes, and output samples.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing filesystem scanner and report model
**Testing**: Temporary mixed-language repositories; full BrownPants suite; live FullMetalFile scan
**Constraints**: Preserve generated-directory exclusions, avoid scanning docs, omit source snippets, and cap file/aggregate reads.

## Design

Analyze the already-discovered source file list using a fixed extension allowlist. Read files in stable path order, skipping files over a per-file cap and stopping at an aggregate byte cap. Match marker tokens with identifier boundaries, count all occurrences in analyzed files, and retain only the requested stable sample. Add a `WorkMarkers` JSON model and Markdown section.

## Validation

- `dotnet build BrownPants.sln --no-restore`
- `dotnet test BrownPants.sln`
- BrownPants default inspect of FullMetalFile and review the archive provider marker location
- `git diff --check`
