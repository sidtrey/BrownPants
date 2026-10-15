# Research: Semantic Resolution Coverage

## Existing behavior

- `RepositoryScanner.Scan` discovers `.csproj` and `.vbproj` files. Default `CSharpAnalyzer` and `VisualBasicAnalyzer` record invocation expression text without binding targets.
- `--semantic` passes discovered projects and solutions to `CSharpSemanticAnalyzer.AnalyzeRepository`.
- When at least one solution is discovered, `AnalyzeRepository` loads only projects contained in those solutions; discovered projects outside all solutions are not attempted.
- A solution can expose the same project more than once, and the current code appends every loaded compilation, which can duplicate declarations and calls.
- `AnalyzeCompilations` resolves C#/VB invocation nodes to `IMethodSymbol`, skips C# `nameof`, counts unresolved bindings, and samples candidate reasons/symbols. Its aggregate count currently does not explain the skipped syntax.
- Semantic JSON contains `TargetPath`/`TargetLine` for in-repository targets. Markdown shows the caller and target symbol but omits those target locations.
- Workspace failures are sampled into `Issues` without a total count. Loaded compilations are not queried for compiler error diagnostics.
- The `--limit` bounds samples, not aggregate call counts. Default mode already emits help and report language distinguishing syntax-only names from semantic resolution.

## Design decisions

1. Preserve the explicit `--semantic` boundary. Do not evaluate projects during ordinary inventory.
2. Load solutions first so their selected project context is honored. Then load discovered project paths not successfully represented in any solution, using a deterministic path set to prevent duplicate analysis.
3. Add separate totals for invocation syntax seen, analyzed calls, resolved calls, unresolved calls, and intentionally skipped calls. Keep count identities exact; do not compare the semantic denominator to default syntax counts because projects may include generated/inactive sources and C#/VB scopes differ.
4. Add target locations to Markdown only when the target source path is repository-contained. Preserve null paths for metadata/external methods.
5. Count workspace/load issues and compiler errors independently from their bounded samples. A compiler error is not a load failure and neither is a confirmed runtime bug.
6. Group all unresolved calls by Roslyn candidate reason and expose only bounded call samples. Sort group names and samples deterministically.
7. Leave JSON schema version 1 unchanged because new semantic properties are additive and existing fields retain their meaning.

## Risks and mitigations

- **Multiple solution configurations**: The same project may be loaded with different properties in different solutions. Analyze it once using the first solution in stable path order and retain the workspace issue/sample for any failed solution; describe the active configuration limitation in reports.
- **Compiler diagnostic cost**: `Compilation.GetDiagnostics()` can add work. It runs only in opt-in semantic mode and results are summarized with bounded samples.
- **Misread unresolved counts**: Keep candidate reason and candidate symbols visible and explicitly state unresolved calls do not prove defects.
- **Generated files**: Existing repository-boundary behavior intentionally includes in-repository generated source trees. Do not silently apply scanner-ignore rules to semantic compilations.
