# Implementation Plan: On-Demand Instruction Sections

**Branch**: `024-lazy-load-context` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: `specs/024-lazy-load-context/spec.md`

## Summary

Extend `brownpants instructions` with explicit single-section retrieval. Reuse recognized/explicit source discovery and anchor rules, then return one Markdown section with repository-relative source identity, stable anchor, 1-based inclusive line range, and selected text in Markdown or JSON. Make heading parsing ignore fenced code blocks. Keep reading offline and read-only; do not select sections automatically or claim control of host context loading.

## Technical Context

**Language/Version**: C# on .NET 10.
**Primary Dependencies**: Existing .NET BCL and current CLI; no new packages.
**Storage**: Repository files only; no persisted data.
**Testing**: xUnit fixture tests in `tests/BrownPants.Tests`.
**Target Platform**: .NET 10 supported Windows, Linux, and macOS environments.
**Project Type**: Command-line application.
**Performance Goals**: A section request reads only the explicitly selected instruction file and emits only the requested section; the inventory remains separately bounded.
**Constraints**: Deterministic Markdown/JSON; no network, project evaluation, code execution, or repository writes. Preserve the existing instruction-source size and reparse-point safeguards. A host may independently preload the same instruction files.
**Scale/Scope**: One source and one selected section per invocation; sources are explicitly requested or recognized by the existing instruction inventory.

## Constitution Check

| Principle | Gate | Plan application |
|---|---|---|
| Evidence First | PASS | Output points to the exact source, anchor, line range, and selected text; no relevance or completeness claims. |
| Broad, Engine-Neutral Core | PASS | CLI output is a general source retrieval contract; no SDD engine or model dependency. |
| Deterministic and Reviewable Output | PASS | Stable Markdown/JSON fields, anchors, and line ranges; fixture tests cover extraction. |
| Safe Inspection by Default | PASS | Read-only, offline, source size and path/reparse-point checks retained; no project evaluation. |
| Specified and Verified Changes | PASS | Feature spec, SHA-256 baseline, testable tasks, xUnit fixtures, and acceptance evidence ledger required. |

No constitution violations or unresolved technical questions remain.

## Design

### Section parsing and retrieval

Replace the heading-only regular-expression scan with a deterministic Markdown line scan shared by instruction inventory and retrieval. Track fenced code blocks opened by backticks or tildes; ignore heading-shaped lines within them. Keep existing anchor normalization and duplicate suffixes so `instructions` output is the lookup index. Store heading line numbers in section records. A requested section includes its heading through the line before the next heading of equal or lesser depth, retaining deeper headings and content. Line ranges are 1-based inclusive; section content is normalized to LF for stable JSON and text output.

### CLI contract and safety

Add `--section <anchor>` to `instructions`; require exactly one `--source <repository-relative-path>` when it is supplied. Resolve and read only that source through `InstructionSourceAnalyzer` so existing size, encoding, repository containment, and reparse-point rules apply. Missing source/anchor and unreadable sources fail without falling back to full inventory or full-file output. Plain inventory behavior remains unchanged except that headings in code fences are no longer listed.

Markdown output includes source, heading, anchor, lines, and a fenced content block with a caveat about host context. JSON uses a versioned result with the same fields and content as Markdown. Help text and Spec Kit context guidance show the command and state that the host must invoke it and may continue loading all instruction files.

## Project Structure

```text
src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs     # Heading metadata and section extraction
src/BrownPants.Cli/Output/InstructionSourceReportRenderer.cs  # Existing inventory plus section rendering
src/BrownPants.Cli/Program.cs                               # instructions --section argument handling
tests/BrownPants.Tests/InstructionSourceAnalyzerTests.cs     # Temporary repository fixtures
.specify/extensions/brownpants/commands/context.md           # Host-neutral retrieval guidance
specs/024-lazy-load-context/                                 # Spec, contracts, and evidence
```

**Structure Decision**: Extend the existing instruction analysis and CLI surfaces; do not add projects, dependencies, or a separate host adapter.

## Constitution Check (Post-Design)

PASS. The output is auditable and bounded, preserves safe-read behavior, and reports a retrieval capability without asserting the host unloaded other context. Fixture tests will cover Markdown/JSON parity, fences, nested and duplicate headings, invalid requests, and path boundaries.
