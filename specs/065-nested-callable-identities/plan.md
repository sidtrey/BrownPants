# Implementation Plan: Nested Callable Identities

**Branch**: `065-nested-callable-identities` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Format semantic caller identities recursively for named methods, local functions, and anonymous functions. Anonymous functions receive a repository-relative source anchor; external/generated paths are redacted. Local functions include their enclosing callable identity. Use the same formatter for declarations and resolved call targets so impact graph joins remain consistent.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing Roslyn compiler symbols and syntax references; no new packages
**Storage**: N/A
**Testing**: xUnit tests in `tests/BrownPants.Tests`
**Target Platform**: .NET 10 supported platforms
**Project Type**: CLI analyzer
**Performance Goals**: Format only observed semantic symbols; no additional project passes
**Constraints**: Deterministic and repository-relative output; do not expose absolute external source paths; preserve ordinary method signatures
**Scale/Scope**: C# and Visual Basic compiler-resolved call and declaration identities

## Constitution Check

- **Evidence First**: Callable identity is source-located compiler context; it does not imply callback execution.
- **Broad, Engine-Neutral Core**: Corrects the shared semantic report consumed by CLI and SDD adapters.
- **Deterministic and Reviewable Output**: Relative paths, one-based coordinates, recursive stable identities.
- **Safe Inspection by Default**: Uses syntax references from the existing opt-in semantic workspace; no new evaluation or file writes.
- **Specified and Verified Changes**: Frozen criteria, regression-first fixtures, output and impact checks, full gates.

All gates pass; no exception is needed.

## Project Structure

```text
src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
tests/BrownPants.Tests/CallImpactAnalyzerTests.cs
README.md
specs/065-nested-callable-identities/
```

**Structure Decision**: Extend the existing shared Roslyn symbol formatter and semantic fixtures; no new project or package.

## Design

Use `IMethodSymbol.MethodKind` to distinguish ordinary methods, local functions, and anonymous functions. Ordinary named methods keep the current fully qualified signature. Local functions recursively include their enclosing callable identity and local signature. Anonymous functions recursively include their enclosing callable identity and a location anchor derived from their declaring syntax reference. Convert source paths to repository-relative form; use a fixed external marker if the path is outside the scan root. Include one-based line and column to distinguish same-line callbacks.

Apply this formatter when producing method declarations, resolved targets, and enclosing callers. Constructor targets retain current spelling. Verify repeated calls inside one lambda share the identity, distinct lambdas do not, C#/VB compilation fixtures pass, and call-impact traversal retains edges.

## Risks

- Roslyn can represent nested lambda scopes as anonymous method symbols; recursive parent formatting must terminate at a named symbol.
- Local functions may be target symbols as well as caller contexts, so formatter consistency is required in both paths.
- External syntax locations must be redacted without causing graph identities to collide; line and column remain part of the anchor.
