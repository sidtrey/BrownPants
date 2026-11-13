# Research: Custom Chart Themes

## Decisions

- **Use BrownPants-owned palette data and existing renderer code.** The CLI already owns layout, PNG drawing/encoding, and diagrams.net XML output in managed C#; adding a graphics package would violate the local-rendering objective and is unnecessary for color substitution.
- **Use strict JSON as the custom theme input.** Agents and developers can create and review a small data file with a familiar format. `System.Text.Json` is already part of .NET 10, so no NuGet package is needed.
- **Keep the file repository-relative and bounded to 16 KiB.** Existing chart input/output paths already enforce repository containment. The same containment and reparse-point checks prevent theme configuration from becoming an arbitrary file-read channel.
- **Use named built-ins plus an optional file.** The built-ins offer one-flag choices; a custom file carries team-specific colors without putting a large JSON value through shell quoting.
- **Keep theme data out of layout.** Existing `LocalFlowchartLayoutEngine` computes graph geometry before renderer output. A theme should alter presentation only.
- **Validate WCAG AA text contrast locally.** The relative luminance formula needs only arithmetic on RGB values; this avoids a dependency and makes rejection deterministic.
- **Preserve imported geometry and non-color styles.** The diagrams.net writer already represents shape styles as style strings. Theme application should replace only recognized color keys and retain unmodeled non-color keys.

## Alternatives considered

- **External Mermaid, diagrams.net, or image library renderer**: rejected because the renderer must remain local and BrownPants-authored, with no chart toolkit.
- **Inline JSON command argument**: rejected because custom palettes are easier to audit in a file and inline JSON is error-prone across PowerShell, Bash, and IDE terminals.
- **Theme can add fonts, icons, gradients, or scripts**: rejected for security, package, and rendering portability reasons; v1 is a validated solid-color palette only.
- **Allow low-contrast colors with a warning**: rejected because unreadable output defeats the usability requirement; errors are reported before files are created.

## Existing repository evidence

- `LocalFlowchartCommand` handles Markdown and diagrams.net imports, tiled outputs, and `.drawio` sidecars.
- `LocalFlowchartPngRenderer` and `LocalFlowchartDrawioWriter` are BrownPants-authored managed code.
- `BrownPants.Cli.csproj` already references `System.Text.Json` through the .NET 10 framework and has no chart-specific package.
- Chart command and renderer behavior is covered by `tests/BrownPants.Tests/LocalFlowchartTests.cs` and method/call chart tests.
