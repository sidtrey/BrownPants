# Plan: C# No-op Implementation Candidates

## Design

Add a dedicated `CSharpImplementationPlaceholderAnalyzer` to BrownPants rather than mixing suspicious behavior with architecture pattern signals or TODO markers. It will parse C# with the existing Roslyn dependency and inspect only method bodies that consist solely of a throw of `NotImplementedException`, an empty body, or a single return/expression resolving syntactically to a clear no-op/default result (`Task.CompletedTask`, `Task.FromResult` of a no-op/default, `Enumerable.Empty`, `Array.Empty`, empty collection construction, null/default/false/zero).

Each result is explicitly a candidate: legitimate APIs can intentionally return empty/default values. Include relative path, source line, containing type, method signature, candidate kind, and a short syntax evidence string. Analyze authored `.cs` files in stable path order, cap source reads at 1 MiB per file, retain up to `sampleLimit`, and report scanned files, bytes, skipped files, total candidates, and omitted candidates.

Add the report as an additive `RepositoryInventory` property with deterministic defaults, wire `RepositoryScanner` to populate it, serialize automatically in JSON, and add a separate Markdown section describing the evidence limits. Do not execute project code or evaluate MSBuild.

## Verification

Use generated temporary C# repositories to test each signal, non-candidates with real bodies/comments/strings, deterministic sampling and omission counts, and the Markdown/JSON inventory surface. Run BrownPants build and focused tests. Run the updated inspector against FullMetalFile to confirm it finds the audit repository no-op methods, then record changed-path impact and feature evidence.
