# Quickstart: Focused Evidence Handoff

From the repository root, request clue-focused JSON:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab `
  --clue "RequiresRetry" --clue "ImportBatch" --format json --limit 1000
```

To include compiler-resolved callers of a changed managed file, opt into semantic project loading:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab/managed `
  --changed Workflows/OrderImportWorkflow.cs --semantic --format json --limit 1000
```

Check `focus` for matched/unmatched selectors, why each finding was selected, whether semantic impact ran, and emitted/omitted counts. Each clue also has up to `min(--limit, 10)` literal path/line matches in `focus.clues[].matches`, even if no candidate finding was selected. `omittedMatchCount`, `clueMatchesTruncated`, and `sourceEvidenceTruncated` show the relevant bounds; use `investigate` when you need a larger literal-location sample. These matches are navigation leads, not proof of a relationship, and do not include source text. Candidate framework, architecture, and test signals remain candidates. Semantic mode loads MSBuild projects; review project files before using it because imported targets may execute.

Run focused tests:

```powershell
dotnet test BrownPants.sln --filter FullyQualifiedName~EvidenceCommandTests
```
