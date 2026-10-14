# Implementation Plan: JVM Factory and Dependency-Injection Signals

**Branch**: `015-jvm-architecture-signals` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/015-jvm-architecture-signals/spec.md`

## Summary

Add deterministic, dependency-free lexical candidates for Java and Kotlin factories and common Spring, Jakarta Inject/CDI, Dagger, and Guice wiring. Mask language-specific comments and literals, retain line locations, and mark Java/Kotlin language coverage as lexical.

## Technical Context

**Language/Version**: C# / .NET 10 analyzer; Java and Kotlin analyzed sources
**Primary Dependencies**: Existing .NET regular expressions and lexical scanning; no JVM packages
**Storage**: Read-only `.java`, `.kt`, and `.kts` files
**Testing**: xUnit temporary-repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: One bounded pass over each source file
**Constraints**: No source execution, JVM build invocation, or semantic graph resolution
**Scale/Scope**: Common factory naming and framework annotations, not a Java/Kotlin parser

## Constitution Check

- Evidence First: all patterns remain `Candidate` signals and docs state limits. PASS.
- Broad, Engine-Neutral Core: adds source evidence for JVM code without SDD-specific behavior. PASS.
- Deterministic and Reviewable Output: existing sort order, report formats, and sample cap apply. PASS.
- Safe Inspection by Default: scanners only read source; no JVM tools are invoked. PASS.
- Specified and Verified Changes: fixture tests cover positives, lexical noise, locations, and output parity. PASS.

## Design

Route `.java`, `.kt`, and `.kts` source paths through a shared JVM candidate analyzer with per-language labels. Mask Java comments and quoted/text-block literals; mask Kotlin line comments, nested block comments, quoted strings, and raw strings while retaining newlines. Match factory names/calls and common injection/provider annotations. Upgrade Java/Kotlin coverage labels and document lexical limits. No report schema change.

## Research

See [research.md](research.md) for primary-source conventions and design limits.

## Project Structure

```text
src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs
src/BrownPants.Cli/Analysis/SourceLanguageCoverageAnalyzer.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
README.md
specs/015-jvm-architecture-signals/
```

## Validation

Run focused JVM fixture tests, Release build and full test suite, Spec Kit/OpenSpec adapter checks, `git diff --check`, then inspect the refreshed BrownPants report and platform CI.
