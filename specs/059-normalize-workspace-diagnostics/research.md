# Research: Workspace Diagnostic Paths

## Observed Evidence

BrownPants-on-BrownPants `inspect --semantic` reported a workspace failure for `examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj`: the project does not contain a `Compile` target. The diagnostic text also included the absolute checkout root, so equivalent reports differ across machines and can reveal local directory names.

The workspace failure is legitimate for this analysis-only fixture. This feature does not attempt to repair project files or suppress the failure; it makes the evidence safe and portable.

## Decisions

- Normalize at the shared `RecordIssue` boundary in `CSharpSemanticAnalyzer.AnalyzeRepository` so all semantic consumers share the behavior.
- Rewrite absolute paths contained by the selected repository root as relative paths with `/` separators.
- Replace recognized absolute paths outside the repository with `[external-path]` so SDK, package-cache, and other host paths do not leak.
- Preserve message text, issue counts, sample caps, completeness state, and public JSON fields.
- Add no dependencies; use path-aware containment checks rather than raw prefix matching.

## Alternatives Considered

- Returning only a generic issue count would discard useful diagnosis and make brownfield analysis harder to act on.
- Keeping absolute paths would make output checkout-specific and unsafe to share.
- Resolving the legacy project's missing build target would change sample project semantics and is outside the analysis/reporting feature.
