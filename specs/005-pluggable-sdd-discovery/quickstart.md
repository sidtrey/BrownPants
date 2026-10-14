# Quickstart: Pluggable SDD Discovery

## Prerequisites

- .NET 10 SDK
- PowerShell 7 for adapter checks

## Validate

1. Create a temporary repository containing `.brownpants/sdd-workflows.json` and files beneath its configured artifact roots.
2. Run `dotnet run --project src/BrownPants.Cli -- inspect <fixture> --format json --limit 2`.
3. Confirm the custom workflow row has correct totals, optional active change count, deterministic paths, and no more than two artifact samples.
4. Run the same fixture as Markdown and confirm it shows the same workflow and any diagnostics.
5. Replace the declaration with invalid JSON, an unknown field, an oversized file, and then a parent traversal root. Confirm each scan reports a diagnostic and no external paths.
6. Run `dotnet test BrownPants.sln` and both adapter scripts:
   - `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1`
   - `pwsh -NoProfile -File tests/OpenSpecInstallerTests.ps1`

## Expected result

Valid custom definitions are reported alongside Spec Kit and OpenSpec. Invalid definitions do not suppress built-in results. Repeated JSON scans of an identical fixture produce identical SDD workflow rows and diagnostics.
