# Verification: Custom Chart Themes

## Acceptance baseline

- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/070-custom-chart-themes/spec.md` — passed; baseline is `8b30d793e140dd3ff5192bf034aac6e171cef010ac47396a40e4bdd8c00a8eee`.
- The baseline includes AC-001 through AC-006 and SC-001 through SC-007. Any change to those requirements needs a reviewed rationale before the digest is refreshed.

## Default-selection parity

- `ChartThemeTests.Diagram_OmittedThemeMatchesExplicitDefaultBytes` compares PNG and XML bytes generated with no theme to bytes generated with `--theme default`.
- A chart-rendering correctness fix now draws arrow tips above node borders and includes the arrow tip pixel. This intentionally updates historical PNG bytes; deterministic output and omitted-versus-explicit default parity remain the compatibility guarantees. Editable XML geometry and relationship data are unchanged.

## Palette model

- Added immutable RGB/theme models and deterministic `default`, `blue-water`, and `high-contrast` palettes in `src/BrownPants.Cli/Analysis/ChartTheme.cs`.
- `ChartThemeTests.ChartThemeCatalog_ProvidesDeterministicAccessiblePalettes` verifies palette names, role coverage, case-insensitive lookup, default colors, and 4.5:1 text contrast for every built-in.
- Focused chart-theme tests pass, 2/2.

## Strict custom-theme parsing

- Added a version-1 parser with strict UTF-8, a 16 KiB bound, required/known property checks, duplicate-key rejection, six-digit RGB parsing, and WCAG text-contrast validation.
- The complete `blue-water.json` fixture parses; tests reject unsupported versions, unknown/duplicate keys, malformed colors, low contrast, oversized input, and invalid UTF-8.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~ChartThemeTests` — passed, 9/9.

## End-to-end verification

- `dotnet build BrownPants.sln -c Release --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-restore` — passed, 368/368.
- `dotnet list BrownPants.sln package --vulnerable --include-transitive` — neither CLI nor test project has vulnerable packages from the configured NuGet source.
- The full suite caught an error-ordering defect: unsupported schema versions were reported as missing other fields. The parser now validates the version first; the invalid-version CLI test confirms the actionable diagnostic and confirms no artifacts are written. The arrowhead rasterizer fix aligns the default palette between source-chart PNG and XML and is covered by seven shape pixel checks.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/070-custom-chart-themes/spec.md` — passed with digest `8b30d793e140dd3ff5192bf034aac6e171cef010ac47396a40e4bdd8c00a8eee`.
- `pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/070-custom-chart-themes/spec.md -TasksPath specs/070-custom-chart-themes/tasks.md -EvidencePath specs/070-custom-chart-themes/acceptance-evidence.json` — passed; 12 tasks map to all 13 criteria.
- `git diff --check` — passed; Git emitted only line-ending conversion warnings for existing modified files.
