# Verification: GitHub Draft Pull Requests

**Date:** 2026-10-15

**Acceptance baseline:** `b12bcdc0c5c9117d209f2cd7ce3120a7bcc46368908540ee7400072f19eb6806`

## Results

- Focused preview, GitHub, and existing Azure DevOps PR tests: **26 passed, 0 failed**. The new GitHub tests use an injected HTTP handler; no live PR was created.
- `dotnet build BrownPants.sln -c Release --no-incremental`: **passed**, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build`: **219 passed, 0 failed**.
- `dotnet pack src/BrownPants.Cli --configuration Release --no-build --output ./artifacts`: **passed**. The packed `0.1.0` tool installed with required package signatures from the configured local NuGet source.
- `tests/PackedGitHubPullRequestSmoke.ps1`: **passed**. The packed tool created an offline preview without Azure DevOps configuration and rejected create before credentials or network access when the explicit gates were omitted.
- `tests/PackedModelRoutingSmoke.ps1`: **passed** for Codex, VS Code, Visual Studio, and Claude profiles.
- `dotnet list BrownPants.sln package --vulnerable --include-transitive`: **no vulnerable packages reported by the configured NuGet source** at verification time.
- BrownPants-on-BrownPants `inspect . --format markdown --limit 10`: **completed**, 1,089 files discovered, 61 C# files parsed, and no Roslyn parse errors. These inventory totals include generated package/test outputs present in the checkout.
- Acceptance criteria hash matched the baseline above; the evidence checker validated **7 tasks and 12 criteria**.
- A high-confidence credential scan of changed feature files found no GitHub/AWS token prefixes, private-key headers, or inline password/client-secret/API-key values. Test token strings are synthetic fixtures.

## Boundaries

The HTTP behavior was verified with mocked responses and the packed CLI's no-network gate; no live GitHub credentials or PR creation were used. GitHub.com draft eligibility, repository policy, and token permission remain provider-controlled. Cross-platform CI is the next environment check after pushing this commit.
