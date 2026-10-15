# Verification: Reproducible Brownfield SDD Comparison

## Acceptance baselines

- Comparison feature acceptance SHA-256: `7c3da79a20458f759f68b18b5ef87eb6077b3323d2de1fdc15a0a877b497c407` (verified).
- Shared bug-fix task acceptance SHA-256: `ee166e7921fa8163d5744acdea4d0e0f2a556f680c96e93edb4b3e05831e61cb` (verified in the assisted worktree).
- Assisted evidence ledger: structurally verified, 3 tasks and 6 criteria. This validator checks structure and paths only.

## Controlled inputs

- Source commit in both formal worktrees: `aaadebfed1d48b6f79babe91e4d4f1418a0e2905`.
- Same task spec, plan, task list, and prompt hashes are recorded in [the comparison report](../../docs/comparisons/brownpants-vs-spec-kit-2026-10.md).
- Same seeded source SHA-256: `38947D3259846093B09C3F5E67ABF0C1505E8273F77C2C1607C96CC429FBAFC5`.
- Same seeded test SHA-256: `3939A3F8A50668AF5DFCB6B30890730461BC989F7A8531FED483DDCF731F01DC`.
- Restore succeeded in both worktrees with `dotnet restore BrownPants.sln --locked-mode`.
- The same focused pre-run check failed in each worktree. It reported one failed test because `tests/fixtures/invoice.json` was missing from the test-path list.

## Agent runs

- Host: Codex CLI `0.162.1`; model: `gpt-6-luna`; Windows; PowerShell `7.6.6`; .NET 10 SDK.
- The baseline agent received only the shared Spec Kit artifacts and task prompt. It restored exact directory-component recognition in `RepositoryScanner.IsTestPath` and reported no test-file edits.
- The assisted agent received the same task artifacts and prompt, plus BrownPants inventory, investigation, and semantic impact reports. It independently restored the same directory-component recognition and reported no test-file edits.
- Each agent changed only `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` relative to the seeded source. The regression assertions were added to both worktrees by the experiment driver before either agent ran.
- Completed Codex command-execution events: 4 baseline; 5 assisted.
- Provider-reported completion usage: baseline 153,864 input (127,232 cached), 1,047 output; assisted 186,445 input (163,072 cached), 1,448 output. Counts came from each completed Codex JSONL `turn.completed.usage` event.

## BrownPants evidence

- `inspect`: 583 files discovered; 69 C# sources.
- `investigate --clue IsTestPath`: 10 literal matches, including the scanner and its investigation/impact consumers.
- `impact --changed src/BrownPants.Cli/Analysis/RepositoryScanner.cs --limit 10000`: analysis complete; 49 direct and 138 transitive caller paths, 20 files, 2 projects, 12 test-path candidates, maximum observed depth 8. These counts cover the scanner file's mapped methods and are not all direct calls to `IsTestPath`.
- Sanitized findings and output hashes are recorded in the [detailed comparison](../../docs/comparisons/brownpants-vs-spec-kit-2026-10.md). Raw report files were kept in the temporary worktree.

## Independent post-run tests

The experiment driver ran these exact commands in each worktree after both agent sessions completed:

- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~RepositoryScannerTests` — **75 passed, 0 failed** in both.
- `dotnet test BrownPants.sln -c Release --no-restore` — **235 passed, 0 failed** in both.

## Run disposition and limits

Two preliminary baseline setup attempts ended without a completed turn record. They are excluded from results and usage totals; both formal worktrees were reset to the common source commit and seeded hashes before measurement. The measured result is one Codex pair for one task. It demonstrates added repository evidence, not general code-quality, productivity, or token savings.

Raw Codex session logs and temporary worktrees are not committed. No credentials or private service data were used in the comparison.
