# Research: Compiler Diagnostic Groups

## Decision

Group all Roslyn error diagnostics by `(repository-relative project path, diagnostic code)`. Retain exact counts for every group, expose only the first `--limit` groups after ordinal sorting, and report exact total and omitted group counts. Preserve existing raw diagnostic samples and their separate totals.

## Rationale

BrownPants' semantic self-report has 21 compiler errors in the legacy Framework fixture. Twelve are `CS0518`, four `CS0549`, three `CS0246`, and two `CS1729`, all in one project. Grouping makes repeated codes visible without discarding line-level evidence. The project/code key distinguishes project hotspots while keeping output deterministic.

## Alternatives considered

- **Replace raw samples with grouped output**: rejected because it removes file, line, and message details needed to investigate a specific error.
- **Group by message text**: rejected because variable type and symbol names can split one code into many rows, and message wording is not a stable identity.
- **Infer likely root causes**: rejected because code frequency alone cannot establish causation, especially when references or target frameworks are unavailable.
- **Add a separate CLI command or option**: rejected because groups are a companion to semantic diagnostics and should be available wherever that report is rendered.

## Repository evidence

`CSharpSemanticAnalyzer.AnalyzeCompilations` walks each loaded compilation's error diagnostics and increments the exact total while retaining `--limit` raw samples. `CSharpSemanticAnalysis` serializes the semantic result, and `MarkdownReport` renders the current total and sample rows. The existing path covers C# and Visual Basic compilations.
