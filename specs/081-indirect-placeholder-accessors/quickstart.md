# Quickstart: Indirect Placeholder Accessors

Validation was run from the BrownPants repository root against `FullMetalFile_React/FullMetalFile.Infrastructure`.

## Results

- `dotnet test BrownPants.sln -c Release --no-build --filter FullyQualifiedName~CSharpImplementationPlaceholderAnalyzerTests`: 6 passed, 0 failed.
- `dotnet build BrownPants.sln -c Release`: passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build`: 430 passed, 0 skipped, 0 failed.
- Parser-backed inspection of the `FullMetalFile.Infrastructure` project: 71 C# files scanned; 6 total candidates, 0 omitted, and 0 skipped.
- In `PostgreSqlUnitOfWork.cs`, the scan reports four indirect repository accessors (`Templates`, `FieldDefinitions`, `TemplateRelationships`, and `DocumentVersions`) and excludes `IndexingSessions`, which is initialized by the constructor. The file also has an empty `Dispose` candidate and the direct `CreateNotImplemented` helper candidate.
- JSON and Markdown output are covered by a generated repository fixture; Markdown labels candidates as methods and properties and explains the one-hop syntax limitation.

See [live-evidence.json](live-evidence.json) for the captured target finding list and scan counts. The earlier five-accessor claim was stale; the evidence-backed wording was approved under the one-maintainer waiver documented in [acceptance-criteria-review.md](acceptance-criteria-review.md). Candidates remain syntax-only review evidence and do not resolve callers, overload binding, inherited members, or runtime dispatch.
