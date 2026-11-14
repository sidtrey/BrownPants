# Implementation Plan: Explicit React Candidate Analysis Status

**Branch**: `078-react-analysis-status` | **Date**: 2026-11-06 | **Spec**: spec.md

## Summary

Add an explicit status to React component and hook-call candidate analysis so reports do not confuse an opt-in parser that did not run with a verified scan that found no candidates. Preserve the safe default and existing parser-based candidates.

## Technical Context

- **Language/Version**: C# 12, .NET 10
- **Parser**: Existing opt-in local TypeScript parser through Node; no package installation or project execution.
- **Output**: Additive JSON status and clearer Markdown status text.
- **Validation**: Focused BrownPants tests for unrequested, verified, unavailable, and failed status; default and parser-verified FullMetalFile scans; acceptance evidence and digest checks.

## Design Decisions

- Add a `CandidateAnalysisStatus` to `ReactSupportAnalysis`; initialize to `not-requested` and map to the existing syntax verifier status when it runs.
- In Markdown, show candidate counts only for verified or partial analysis. For not-requested, unavailable, or failed analysis, explain that candidate counts are unavailable and how to request verification.
- Keep React manifest detection active in default scans; do not invoke Node unless `--typescript-syntax` is supplied.
- Keep existing JSON count fields for compatibility, with the new status providing the required interpretation.

## Project Structure

```text
src/BrownPants.Cli/Analysis/JavaScriptTypeScriptModels.cs
src/BrownPants.Cli/Analysis/JavaScriptTypeScriptAnalyzer.cs
src/BrownPants.Cli/Output/MarkdownReport.cs
tests/BrownPants.Tests/TypeScriptSyntaxVerifierTests.cs
specs/078-react-analysis-status/
```

## Risks

- Existing JSON consumers may ignore the added status property and still interpret zero counts literally. Keeping counts additive and preserving their values avoids breaking current readers; Markdown must make the status prominent.
- A verified parser status is syntax evidence only and must not be described as runtime component discovery.
