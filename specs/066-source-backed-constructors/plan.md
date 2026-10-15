# Implementation Plan: Source-Backed Constructor Declarations

**Branch**: `066-source-backed-constructors` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Extend the Roslyn declaration map to include compiler-bound implicit and primary constructors anchored to their source type declarations. Keep constructor call target formatting unchanged so impact traversal can join construction edges to changed source files.

## Technical Context

**Language/Version**: C# / .NET 10; Roslyn C# and Visual Basic
**Primary Dependencies**: Existing Roslyn compiler symbols and syntax references; no new packages
**Storage**: N/A
**Testing**: xUnit semantic compilation fixtures
**Target Platform**: .NET 10 supported platforms
**Project Type**: CLI analyzer
**Performance Goals**: Linear additional scan over type declarations and their constructor symbols
**Constraints**: Preserve explicit constructor identities and counts; emit only repository source locations; maintain deterministic ordering
**Scale/Scope**: C#/VB source constructors and managed impact traversal

## Constitution Check

- **Evidence First**: Only compiler-bound constructor symbols are represented; source locations are not runtime proof.
- **Broad, Engine-Neutral Core**: The shared semantic graph remains usable by all report and SDD adapters.
- **Deterministic and Reviewable Output**: Reuse canonical identities and source-relative paths; avoid duplicate declarations.
- **Safe Inspection by Default**: No change to opt-in MSBuild semantic loading or default static inspection.
- **Specified and Verified Changes**: Hashed ACs, focused fixtures, complete release gates, and Bug Lab checks are required.

All gates pass; no exception is needed.

## Project Structure

```text
src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
tests/BrownPants.Tests/CallImpactAnalyzerTests.cs
README.md
specs/066-source-backed-constructors/
```

**Structure Decision**: Extend semantic declaration collection and existing call impact traversal. No new project, package, or report schema is required.

## Design

For each C# and Visual Basic type declaration, obtain its `INamedTypeSymbol` and inspect instance constructor symbols. Explicit constructors already discovered as method syntax remain unchanged. For constructors without an explicit constructor declaration node, select a source location from the symbol's syntax references or source locations, and use the containing type declaration as a fallback for implicit constructors. A C# primary constructor is anchored at its type declaration. Skip constructors with no source location inside the repository. Deduplicate by project, path, line, and canonical constructor identity.

The existing constructor relationship target is the canonical `ContainingType..ctor(parameter-signatures)` string. Use it unchanged for both the synthesized declaration entry and resolved constructor edges. This allows the existing changed-file seed and breadth-first impact traversal to find callers without introducing a second constructor model.

## Risks and Mitigations

- Roslyn represents primary and implicit constructor syntax locations differently by language; add fixtures that assert actual target/declaration identity and path/line matching.
- Records may expose multiple compiler-generated constructors; deduplicate symbols and include only constructors with a usable source anchor.
- Existing explicit constructors could be emitted twice by type enumeration; skip syntax-backed explicit declarations or deduplicate before counts and samples are finalized.
- Constructor source links do not establish execution or DI activation; preserve current evidence labels and limitations.
