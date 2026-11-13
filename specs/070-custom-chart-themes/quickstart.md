# Custom Chart Themes Quickstart

## Prerequisites

- .NET 10 SDK and a built BrownPants CLI.
- A repository containing a supported chart source and, for custom colors, a UTF-8 JSON theme file inside that repository.

## Built-in theme

```powershell
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/access-review.md `
  --output TestResults/access-review-blue.png --theme blue-water
```

Expected: a PNG, matching editable `.drawio` sidecar, and a concise summary. Repeating the command with the same source and options produces identical bytes.

## Custom theme

Create `docs/themes/blue-water.json` using the [theme schema](contracts/chart-theme.schema.json), then run:

```powershell
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/access-review.md `
  --output TestResults/access-review-custom.png --theme-file docs/themes/blue-water.json

dotnet run --project src/BrownPants.Cli -- chart callers . `
  --source src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs `
  --method 'global::BrownPants.Cli.Analysis.LocalFlowchartPngRenderer.Render(global::BrownPants.Cli.Analysis.LocalFlowchartDocument, global::BrownPants.Cli.Analysis.LocalFlowchartLayoutOptions, global::BrownPants.Cli.Analysis.ChartTheme)' `
  --semantic --output TestResults/callers-custom.png --theme-file docs/themes/blue-water.json

dotnet run --project src/BrownPants.Cli -- chart flow . `
  --source src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs `
  --method 'global::BrownPants.Cli.Analysis.LocalFlowchartCommand.ResolveInside(string, string, string)' `
  --semantic --output TestResults/method-flow-blue.png --theme blue-water
```

The CLI validates the file and text contrast before writing PNG/XML output. The file path must remain inside the selected repository. Invalid colors, missing roles, low contrast, and unsupported schema versions return actionable errors and create no output.

## Source charts

Add the same option to `chart callers` or `chart flow`; all generated tiles and editable sidecars use the selected palette. Compare output dimensions and graph elements to the default render to confirm theme changes affect presentation only.
