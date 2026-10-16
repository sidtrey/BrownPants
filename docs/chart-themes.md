# Local Chart Themes

BrownPants can apply a built-in or agent-created palette to a chart's PNG and editable diagrams.net file. Theme selection changes color only; it does not change node positions, labels, edges, routes, or what the chart claims. Rendering stays in BrownPants' managed .NET code and does not load fonts, scripts, images, or remote resources.

## Choose a built-in palette

Use `--theme` with `default`, `blue-water`, or `high-contrast`:

```powershell
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/access-review.md `
  --output TestResults/access-review-blue.png --theme blue-water
```

The same option works with `chart callers` and `chart flow`. All tiles and their XML sidecars receive the selected palette. For BrownPants-authored charts, omitting the option uses the `default` palette and produces the same PNG/XML bytes as `--theme default`. Renderer correctness fixes can change historical image bytes; identical inputs and options remain deterministic. Imported diagrams keep their original XML colors unless a theme is explicitly requested.

## Provide a custom palette

An agent can create a small JSON file, review it, and pass its repository-relative path. The checked-in [Blue Water example](themes/blue-water.json) uses schema version 1:

```json
{
  "schemaVersion": 1,
  "name": "Blue Water",
  "canvas": "#EAF6FA",
  "text": "#16323D",
  "connector": "#28758C",
  "edgeLabelBackground": "#FFFFFF",
  "edgeLabelText": "#16323D",
  "shadow": "#C9E1E7",
  "shapes": {
    "start": { "fill": "#CDEFE3", "stroke": "#1F6475" },
    "process": { "fill": "#FFFFFF", "stroke": "#1F6475" },
    "decision": { "fill": "#CDE8F5", "stroke": "#1F6475" },
    "inputOutput": { "fill": "#C1E4EA", "stroke": "#1F6475" },
    "person": { "fill": "#DCEEF3", "stroke": "#1F6475" },
    "cloud": { "fill": "#E3F2F2", "stroke": "#1F6475" },
    "end": { "fill": "#CDEFE3", "stroke": "#1F6475" }
  }
}
```

Every color is six-digit `#RRGGBB`. All fields and shape roles are required. Node text and edge-label text must meet a 4.5:1 contrast ratio against their backgrounds. BrownPants rejects duplicate or unknown properties, unsupported versions, malformed colors, unreadable contrast, invalid UTF-8, and files larger than 16 KiB before writing output. Theme files must remain inside the selected repository and cannot pass through symbolic links.

```powershell
dotnet run --project src/BrownPants.Cli -- diagram . docs/flows/access-review.md `
  --output TestResults/access-review-custom.png --theme-file docs/themes/blue-water.json

dotnet run --project src/BrownPants.Cli -- chart callers . --source src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs `
  --method 'global::BrownPants.Cli.Analysis.LocalFlowchartPngRenderer.Render(global::BrownPants.Cli.Analysis.LocalFlowchartDocument, global::BrownPants.Cli.Analysis.LocalFlowchartLayoutOptions, global::BrownPants.Cli.Analysis.ChartTheme)' `
  --semantic --output TestResults/callers.png --theme-file docs/themes/blue-water.json

dotnet run --project src/BrownPants.Cli -- chart flow . --source src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs `
  --method 'global::BrownPants.Cli.Analysis.LocalFlowchartCommand.ResolveInside(string, string, string)' `
  --semantic --output TestResults/method-flow.png --theme blue-water
```

`--theme` and `--theme-file` are mutually exclusive. Theme values only control presentation; inspect the chart evidence and its stated analysis limits as usual.
