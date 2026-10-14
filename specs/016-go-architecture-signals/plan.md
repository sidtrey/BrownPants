# Implementation Plan: Go Factory and Dependency-Injection Signals

**Branch**: `016-go-architecture-signals` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/016-go-architecture-signals/spec.md`

## Summary

Add dependency-free lexical candidates for idiomatic Go constructor/factory functions and common Uber Fx/Dig calls. Mask Go comments and string/rune/raw-string literals, preserve source lines, and upgrade Go language coverage.

## Technical Context

**Language/Version**: C# / .NET 10 analyzer; Go source analyzed lexically
**Primary Dependencies**: Existing .NET regular expressions and lexical scanner
**Storage**: Read-only `.go` files
**Testing**: xUnit temporary-repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: One bounded pass over each source file
**Constraints**: No Go toolchain invocation, source execution, or graph resolution
**Scale/Scope**: Common `NewType` names and Fx/Dig calls, not a Go parser

## Constitution Check

- Evidence First: all lexical findings remain `Candidate`; docs describe naming and framework limits. PASS.
- Broad, Engine-Neutral Core: adds Go evidence without SDD-specific scanner behavior. PASS.
- Deterministic and Reviewable Output: existing path ordering, line locations, and sample cap apply. PASS.
- Safe Inspection by Default: source is read only; no Go commands are invoked. PASS.
- Specified and Verified Changes: fixtures cover positives, noise filtering, locations, and output parity. PASS.

## Design

Route `.go` files through a lexical analyzer that masks `//`, `/* */`, interpreted strings, rune literals, and raw backtick strings while preserving newlines. Match `NewType` and common factory function declarations/calls, Fx/Dig `Provide`/`Invoke`/container creation cues, and `Factory` type declarations. Upgrade Go coverage and document candidate limits. Do not change report schema.

## Research

See [research.md](research.md) for primary-source Go naming and Fx/Dig API conventions.

## Project Structure

```text
src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs
src/BrownPants.Cli/Analysis/SourceLanguageCoverageAnalyzer.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
README.md
specs/016-go-architecture-signals/
```

## Validation

Run focused Go fixture tests, Release build and full suite, Spec Kit/OpenSpec adapter checks, `git diff --check`, then review the refreshed BrownPants report and platform CI.
