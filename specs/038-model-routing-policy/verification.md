# Verification: Task-Based Model Guidance

**Date:** 2026-10-15

**Acceptance baseline:** `9db28707e8d0f1125d5f02dee9f52c02bfbd723b7fe9a921970c04942ea432dc`

## Results

- Focused model-routing tests: **27 passed, 0 failed**.
- `dotnet build BrownPants.sln -c Release --no-incremental`: **passed**, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build`: **201 passed, 0 failed**.
- `tests/PackedModelRoutingSmoke.ps1` against the locally packed tool: **passed**. The sample policy validated; each of Codex, VS Code, Visual Studio, and Claude exported all three profiles. Visual Studio output used its distinct `.github/agents/brownpants-visual-studio-<id>.agent.md` names and host-specific display name.
- `dotnet list BrownPants.sln package --vulnerable --include-transitive`: **no vulnerable packages reported by the configured NuGet source** at verification time.
- BrownPants-on-BrownPants `inspect . --format markdown --limit 10`: **completed**, inventory found 1,071 files, parsed 56 C# files, and reported no Roslyn parse errors. The report also identified generated package/test output in the working tree, so inventory counts include those present artifacts.
- `scripts/acceptance-criteria-hash.ps1`: **passed**, digest matches the accepted baseline above.
- `scripts/check-acceptance-evidence.ps1`: **passed**, 7 tasks and 16 criteria mapped. The checker validates structure and evidence paths; test results above provide the semantic verification.

## Scope limits

Visual Studio support targets 18.4 and later according to its documented custom-agent format. This verification exercised generated files through the packed CLI, not activation inside Visual Studio or VS Code. Host sign-in, model availability, and runtime delegation remain controlled by each host and account.
