# Verification Evidence: Multi-Provider Draft Pull Requests

Date: 2026-10-15

- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/044-multi-provider-draft-prs/spec.md` — PASS; SHA-256 `d189f3ad4d4be4e4269882b6531b4f65e9b1fec7ca74d955606ded746eeb18e7` before and after implementation.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj --no-restore --filter FullyQualifiedName~MultiProviderPullRequestTests` — PASS; 6/6 provider tests.
- `dotnet build BrownPants.sln -c Release --no-restore` — PASS; 0 warnings, 0 errors.
- `dotnet test BrownPants.sln -c Release --no-restore` — PASS; 235/235 tests.
- `dotnet list BrownPants.sln package --vulnerable --include-transitive` — PASS; no vulnerable packages reported.
- `dotnet run --project src/BrownPants.Cli -c Release --no-build -- pr create --help` — PASS; provider usage and both explicit gates displayed.
- `git diff --check` — PASS; only a line-ending normalization warning for `Program.cs`.

Provider tests use injected HTTP handlers and synthetic credentials. No provider was contacted and no live credential was used. Existing GitHub/Azure preview and submission tests are included in the passing full suite.
