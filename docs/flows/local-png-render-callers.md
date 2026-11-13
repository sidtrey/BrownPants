# BrownPants self-analysis: PNG renderer callers

This diagram was generated from BrownPants itself with Roslyn semantic analysis. It shows five direct callers of `LocalFlowchartPngRenderer.Render`, including three test-path candidates.

The run uses depth 1, so deeper callers are not included. BrownPants also reported 13 unresolved delegate targets across the loaded repository; additional delegate-based callers may be missing. Candidate test paths do not prove runtime coverage. Review the editable [diagrams.net source](local-png-render-callers.drawio) to inspect full compiler symbols and source paths in node tooltips.

```powershell
$method = 'global::BrownPants.Cli.Analysis.LocalFlowchartPngRenderer.Render(global::BrownPants.Cli.Analysis.LocalFlowchartDocument, global::BrownPants.Cli.Analysis.LocalFlowchartLayoutOptions, global::BrownPants.Cli.Analysis.ChartTheme)'
dotnet run --project src/BrownPants.Cli -- chart callers . `
  --source src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs `
  --method $method --semantic --output docs/flows/local-png-render-callers.png `
  --depth 1 --limit 1000 --direction left-to-right --overwrite
```

`--semantic` loads project files, and imported MSBuild targets may execute. Run it only where repository and policy allow that evaluation.
