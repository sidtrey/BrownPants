# Verification: Vanilla SDD and BrownPants Comparison

**Date:** 2026-10-15

**Implementation base:** `7a3950825dce2db0b88b951480942d6c52485813`

**Feature acceptance SHA-256:** `569f0f6baa65f8c3827716609c68fd1b208132b8843ae13ec76e424307793077`

**Controlled-task acceptance SHA-256:** `ee43ce2376df97ca44a3bdb497aef2b23cadb91206356a1fa9aa9fdcf7599296`

## Paired comparison

The usable matched pair used Codex CLI `0.162.1`, model `gpt-6-luna`, Windows, PowerShell 7.6.6, and .NET SDK `10.0.402`. Both detached worktrees used the same base commit and an identical untracked `src/ContestController.cs` fixture. Both model runs received the same hash-pinned task; only the BrownPants condition received the bounded Markdown inventory. The JSONL usage parser observed:

| Run | Input tokens | Cached input | Output tokens | Shell invocations | Sandbox-blocked |
|---|---:|---:|---:|---:|---:|
| Vanilla | 336,632 | 297,472 | 2,688 | 9 | 2 |
| BrownPants-assisted | 260,164 | 237,056 | 2,234 | 8 | 4 |

Both drafts changed the scanner and one test file. Vanilla added suffix-only test-name matching; BrownPants-assisted added prefix and suffix matching. Neither draft was committed. Their Codex sandboxes blocked NuGet/telemetry access, so the maintainer ran the same commands directly in each worktree:

- `dotnet restore BrownPants.sln --locked-mode -p:NuGetAudit=false --ignore-failed-sources` — passed in both worktrees.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~RepositoryScannerTests` — 75/75 passed in both.
- `dotnet test BrownPants.sln -c Release --no-restore` — 229/229 passed in both.

Review found that both drafts still classify `Latest.cs` as a test; the assisted draft also classifies `Testimonial.cs`. The release implementation adds boundary-aware matching and checks both ordinary names and conventional test names.

## Incomplete attempts and usage

The first vanilla attempt completed with 646,942 input / 4,409 output tokens. Its paired BrownPants attempt stopped before repository inspection with `orchestrator_helper_incomplete`, reporting 121,284 input / 475 output tokens. A later vanilla-only attempt was interrupted after sandbox network blocks; it had no `turn.completed` usage event, so its token totals are unavailable. It was not presented as a paired result. These attempts are detailed in the comparison report and excluded from the matched result.

## Repository release checks

- `dotnet build BrownPants.sln -c Release --no-restore` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-restore` — passed, 229/229.
- `pwsh -NoProfile -File tests/CodexUsageParserTests.ps1` — passed.
- `dotnet list BrownPants.sln package --vulnerable --include-transitive` — no vulnerable packages reported.
- `dotnet run --project src/BrownPants.Cli --no-build -c Release -- inspect . --format json --limit 10` — 555 files, 67 C# source files, 0 C# parse errors; generated `TestResults` is absent from top-level inventory.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/043-sdd-head-to-head/spec.md` — verified the feature hash above.
- GitHub Actions on base commit `7a3950825dce2db0b88b951480942d6c52485813` — Windows, macOS, and Ubuntu CI all passed.

The remaining release check is CI for the final implementation commit.
