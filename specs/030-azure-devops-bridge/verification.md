# Verification: Azure DevOps Bridge

## Local validation

Environment: Windows, .NET 10, 2026-10-14.

- `dotnet build BrownPants.sln -c Release` — passed with zero warnings and zero errors.
- `dotnet test BrownPants.sln -c Release --no-build` — passed: 140 tests after final branch-name and response-validation changes.
- `dotnet pack src/BrownPants.Cli -c Release --no-build --output ./artifacts` — passed. The installed .NET tool ran global help, Azure DevOps PR help, and JSON `inspect` smoke checks.
- `scripts/acceptance-criteria-hash.ps1` — passed with digest `a7edacc2a80cdd97f8ba811147f9335a7b821375ede4119f0e228a52ff5e1d84`.
- `scripts/check-acceptance-evidence.ps1` — passed structurally: 20 tasks and 18 criteria.
- BrownPants self-inspection — `dotnet run --project src/BrownPants.Cli -- inspect . --format markdown --limit 10` completed and found no Roslyn parse errors.

## CI follow-up

The repository CI matrix runs the xUnit suite on Windows, Ubuntu, and macOS. Cross-platform results are pending the push that triggers CI; keep task T017 open until those runs finish successfully.

## Live-service limits

Azure DevOps calls were validated with mocked HTTP handlers. No organization credentials or live Azure DevOps mutation were used. Browser sign-in still requires a tenant-configured public client and user-granted Azure DevOps access; creating a draft PR requires Azure Repos code contribution permission.
