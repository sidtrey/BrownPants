# Implementation Plan: Report Python Factory and DI Signals

**Branch**: `013-python-architecture-signals` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/013-python-architecture-signals/spec.md`

## Summary

Extend architecture candidate reporting to Python using a bounded, dependency-free lexical scan. Recognize common factory and DI patterns, mask comments and strings, and update the Python analysis level in reports.

## Technical Context

**Language/Version**: C# / .NET 10; analyzed source is Python 3
**Primary Dependencies**: Existing .NET regular expressions and scanner
**Storage**: Read-only Python files in the scanned repository
**Testing**: xUnit temporary repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: One bounded lexical pass per Python file; do not invoke Python
**Constraints**: Filter comments and single/triple-quoted strings; do not claim semantic results
**Scale/Scope**: Factory naming, Dependency Injector providers, FastAPI dependencies; not a Python grammar

## Constitution Check

- Evidence First: all findings remain explicitly labeled candidate patterns. PASS.
- Broad, Engine-Neutral Core: adds non-.NET source evidence usable by any SDD engine. PASS.
- Deterministic and Reviewable Output: existing sort, limits, paths, and lines apply. PASS.
- Safe Inspection by Default: read-only, no interpreter, network, or package execution. PASS.
- Specified and Verified Changes: fixture tests cover positive patterns and lexical noise. PASS.

## Design

Route `.py`, `.pyw`, and `.pyi` files through a Python-specific lexical pattern pass. Mask string literals and `#` comments while preserving line positions, including multiline triple quotes and prefixed strings. Emit architecture candidates for factory class/method/call conventions, `providers.Factory`/`Singleton` declarations, and dependency markers such as `Depends`, `Provide[...]`, and `@inject`. Change Python's coverage row from inventory-only to lexical candidate patterns. Do not add parser or report-schema dependencies.

## Validation

Run the full Release build and xUnit suite, review Markdown and JSON parity and repeatability, run Spec Kit and OpenSpec adapter checks, and inspect report language coverage.
