# Research: Call-Trace Change Impact Report

## Decision: Trace reverse edges from changed declarations

The semantic analyzer already reports caller and target symbols for resolved C# and VB.NET invocation expressions, plus invocation file and line. Its sample lists are capped, while resolved/unresolved counters reflect all observed invocations. BrownPants' own semantic scan loaded both projects, resolved 3,324 calls, reported no unresolved calls or workspace issues, and sampled 100 calls under `--limit 100`.

Add source declaration locations to resolved targets. For each changed source path, identify loaded declarations in that file, then traverse resolved incoming edges breadth-first. Deduplicate callers and keep the shortest discovered depth. The impacted location is the call site, which points reviewers to the dependent code.

## Decision: Treat coverage and complexity as observed graph evidence

Mark impact incomplete when project loading is partial, unresolved calls exist, samples omit edges, or changed inputs cannot map to loaded methods. Report callers, paths, projects, test-candidate paths, and maximum observed depth. Do not infer tests' assertions or forecast implementation effort.

## Alternatives considered

- Match changed files to target names from text signatures: rejected because overloads, namespaces, and aliases can produce incorrect matches.
- Automatically infer changed files from Git: rejected for the first command; explicit paths make the reviewed input visible and work without Git history.
- Treat the graph as complete when zero callers are found: rejected because reflection, dynamic dispatch, native interop, and incomplete project loads can hide relationships.

## Repository evidence

- `CSharpSemanticAnalyzer` resolves call targets for both C# and VB.NET and records the invocation location.
- `RepositoryInventory.TestPaths` already identifies likely test paths heuristically.
- `RepositoryScanner.Scan(..., analyzeSemantics: true)` is read-only by default apart from MSBuild's documented design-time loading behavior.