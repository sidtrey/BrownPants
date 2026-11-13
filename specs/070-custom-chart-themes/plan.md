# Implementation Plan: Custom Chart Themes

**Branch**: `070-custom-chart-themes` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/070-custom-chart-themes/spec.md`

## Summary

Add deterministic built-in and custom color palettes to the local chart commands. Parse a bounded repository-relative JSON theme with `System.Text.Json`, validate its schema and text contrast, and pass an immutable theme value through the existing managed PNG renderer and diagrams.net XML writer. The default palette and unthemed output remain unchanged.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing .NET libraries, `System.Text.Json`; no new NuGet package
**Storage**: Read-only local repository theme JSON; PNG/XML outputs use existing path-safe writer
**Testing**: xUnit fixture and CLI integration tests in `tests/BrownPants.Tests/`
**Target Platform**: .NET 10 on Windows, Linux, and macOS
**Project Type**: .NET CLI
**Performance Goals**: Parse at most 16 KiB once per invocation; rendering cost remains dominated by existing chart size
**Constraints**: Theme path must remain inside selected repository; reject reparse-point traversal, external resources, malformed JSON, duplicate/unknown properties, unsupported versions, and low contrast before writing outputs. No project evaluation is added to theme loading.
**Scale/Scope**: Three named palettes; one optional custom file; seven semantic node shapes and shared canvas/text/connector/edge-label/shadow roles.

## Constitution Check

- **Evidence First**: Diagnostics name the theme file and invalid role; theme colors describe presentation only and cannot imply source semantics.
- **Broad, Engine-Neutral Core**: Themes apply to BrownPants' chart formats without depending on Spec Kit, OpenSpec, or an AI provider.
- **Deterministic and Reviewable Output**: Strict JSON schema, exact colors, and repeat-output tests.
- **Safe Inspection by Default**: Theme files are bounded, declarative, repository-relative, and never execute code or load resources.
- **Specified and Verified Changes**: Hashed acceptance criteria, fixture tests, docs, and full build/test checks are required.

**Gate**: Pass. The design adds no dependency or network behavior and preserves unthemed output.

## Design

### Theme contract

`contracts/chart-theme.schema.json` defines schema version 1. Required roles are RGB hex colors for the canvas, shared text, connector, edge-label background/text, shadow, and each supported shape's fill/stroke. Values use six-digit `#RRGGBB`. Unknown or duplicate properties fail. The custom file is capped at 16 KiB and uses no references, includes, fonts, images, CSS, or URLs.

Named themes are `default`, `blue-water`, and `high-contrast`. A shared immutable `ChartTheme` model represents built-in and custom palettes. `default` resolves to the existing default palette constants, and omitted selection must match explicit `--theme default` byte-for-byte. A renderer correctness fix may update historical image bytes. WCAG relative luminance is calculated locally; node text against every shape fill and edge-label text against its background must reach 4.5:1.

### Command and output flow

`diagram`, `chart callers`, and `chart flow` accept mutually exclusive `--theme <name>` and `--theme-file <relative-json-path>`. The theme is parsed before any output staging begins. It is passed into shared PNG drawing and XML style generation; the layout engine receives no theme values, preventing color choices from changing placement or routing. For imported diagrams, theme roles replace only supported fill, stroke, text, connector, and label-background style values; geometry, shape, labels, edges, and waypoints remain input-defined.

### Repository structure

```text
src/BrownPants.Cli/Analysis/ChartTheme.cs
src/BrownPants.Cli/Analysis/ChartThemeParser.cs
src/BrownPants.Cli/Analysis/LocalFlowchartCommand.cs
src/BrownPants.Cli/Analysis/LocalFlowchartPngRenderer.cs
src/BrownPants.Cli/Analysis/LocalFlowchartDrawioWriter.cs
src/BrownPants.Cli/Analysis/CallTreeDiagramCommand.cs
src/BrownPants.Cli/Analysis/MethodControlFlowDiagramCommand.cs
tests/BrownPants.Tests/ChartThemeTests.cs
tests/BrownPants.Tests/Fixtures/ChartThemes/*.json
docs/chart-themes.md
```

## Risks and Mitigations

- Custom palettes can be unreadable: enforce contrast before output and test every built-in.
- Imported styles may contain unsupported values: change only recognized color keys and retain non-color style tokens.
- Omitted and explicit default selection can drift: compare their PNG/XML bytes directly and retain deterministic repeat checks.
- Theme behavior can diverge across chart commands: use one model/parser and exercise every command path.
