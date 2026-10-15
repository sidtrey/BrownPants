# Implementation Plan: Normalize Workspace Diagnostics

**Branch**: `059-normalize-workspace-diagnostics` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Normalize MSBuildWorkspace diagnostic strings before they enter `CSharpSemanticAnalysis.Issues`. Preserve repository-relative file references and the failure reason, replace absolute paths outside the scan root with `[external-path]`, and leave issue counts, sampling, completeness, and public JSON shape unchanged.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing .NET path APIs and regular expressions; no new packages
**Storage**: None
**Testing**: xUnit with deterministic path-message fixtures and semantic JSON serialization
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local repository-analysis CLI
**Performance Goals**: Linear work in the bounded workspace-issue sample
**Constraints**: Use the selected scan root as the only path disclosure boundary; normalize separators to `/`; preserve diagnostic causes and schema
**Scale/Scope**: Bounded MSBuild/Roslyn workspace issue messages captured during semantic project loading

## Constitution Check

- **Evidence First**: Pass. Preserve the real workspace cause and identify internal paths as relative; external paths are redacted rather than misrepresented.
- **Broad, Engine-Neutral Core**: Pass. The change is in shared semantic reporting, independent of SDD adapters.
- **Deterministic and Reviewable Output**: Pass. Identical repository-relative diagnostics produce identical strings across checkout roots.
- **Safe Inspection by Default**: Pass. No project loading behavior changes; semantic analysis remains opt-in.
- **Specified and Verified Changes**: Pass. Frozen criteria cover path redaction, determinism, and schema/count compatibility.

## Research and Design Decisions

- `CSharpSemanticAnalyzer.AnalyzeRepository` collects workspace failures through one `RecordIssue` function. Normalize at this boundary so `inspect`, `evidence`, and any semantic consumers receive the same safe text.
- A current self-run issue identifies `examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj` and says that the project does not contain a `Compile` target. The raw message currently embeds the checkout's absolute path.
- Replace in-root absolute paths with repository-relative paths. Replace any recognized absolute path outside the root with `[external-path]`. Do not hide explanatory message text.
- Keep diagnostics as strings and preserve the existing `CSharpSemanticAnalysis` JSON property set. Add no public fields or CLI options.
- Test path-prefix collision, external paths, different checkout roots, and serialization. Existing workspace failure counters and bounded sampling remain authoritative.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── CSharpSemanticAnalyzer.cs
└── WorkspaceDiagnosticNormalizer.cs

tests/BrownPants.Tests/RepositoryScannerTests.cs
specs/059-normalize-workspace-diagnostics/
```

**Structure Decision**: Add a small internal normalizer beside semantic analysis and test it with the existing test assembly's internal access. No new package or runtime project.

## Risks and Mitigations

- Path-like text can occur in non-path diagnostics. Normalize only recognized absolute path tokens; preserve all other text.
- A sibling directory may share the root's string prefix. Determine containment by path segments, not string prefix alone.
- Workspace messages may contain SDK paths outside the repository. Replace those path tokens with a stable marker and retain the reason.

## Complexity Tracking

No constitution violations or new runtime dependencies.
