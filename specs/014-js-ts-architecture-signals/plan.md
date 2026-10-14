# Implementation Plan: Report JavaScript and TypeScript Factory and DI Signals

**Branch**: `014-js-ts-architecture-signals` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/014-js-ts-architecture-signals/spec.md`

## Summary

Extend the architecture analyzer with dependency-free lexical candidates for JavaScript and TypeScript factories and common Angular/Inversify dependency-injection cues. Filter comments and strings, preserve line locations, and upgrade both language coverage levels.

## Technical Context

**Language/Version**: C# / .NET 10; analyzed sources are JavaScript and TypeScript
**Primary Dependencies**: Existing .NET regular expressions and lexical scanner
**Storage**: Read-only `.js`, `.jsx`, `.mjs`, `.cjs`, `.ts`, `.tsx`, `.mts`, and `.cts` files
**Testing**: xUnit temporary repositories in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: One bounded pass over each source file; no package or runtime invocation
**Constraints**: Mask line/block comments and quoted/template literals; do not claim semantic resolution
**Scale/Scope**: Common factory naming and Angular/Inversify DI markers; not full JavaScript/TypeScript parsing

## Constitution Check

- Evidence First: all lexical matches remain `Candidate` findings with limits. PASS.
- Broad, Engine-Neutral Core: adds useful evidence for common web/Node repositories. PASS.
- Deterministic and Reviewable Output: existing sorting, sample limits, paths, and lines apply. PASS.
- Safe Inspection by Default: reads source only and executes no project code. PASS.
- Specified and Verified Changes: fixture tests cover positive and negative evidence in both formats. PASS.

## Design

Route the existing JavaScript and TypeScript extensions through one language-aware lexical pass. Preserve `JavaScript` and `TypeScript` labels per extension. Filter `//`, `/* */`, single/double quotes, and multiline template literals before matching factory class/function/method/call names and the specified DI markers. Normalize LF/CRLF line endings for stable locations. Change both language coverage levels to lexical candidate patterns and update the README/report wording. Do not add a parser or report schema.

## Validation

Run the Release build and xUnit suite, verify Markdown/JSON record parity and determinism, run Spec Kit and OpenSpec adapter checks, and inspect the cross-platform CI matrix.
