# Feature 067 Verification

## Frozen acceptance inputs

- Feature criteria digest verified unchanged: `fac2f20e2e615324c06ce01b3085444948d17ad4f58c05b8cce016ca9a2150bc`.
- Delegate benchmark task digest: `3d5469a244e1a08c602e01b140af98da5c89d165b83e89b201a00493565032f3`.
- Answer-key digest: `eb212b96228435f94b0d9d4396964d5eb8a6b0c48ebf79d65e3560d5ab0a9fc0`.
- Frozen protocol inputs pass `Verify-FrozenInputs.ps1`; the scorer self-test passes exact matches, normalized labels/signatures/paths, duplicates, omissions, and decoys.

## Build and test results

- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 299 tests, 0 failed, 0 skipped.
- Focused delegate tests — passed for C# method groups, selected overloads, lambdas, explicit `Invoke`, VB method groups, impact traversal, unresolved reassigned/captured/escaped/field/parameter flows, external-path redaction, deterministic output, and evidence rendering.
- `dotnet run --project examples/brownfield-bug-lab/managed/tests/LegacyOrderDesk.Checks.csproj -c Release` — passed: `LegacyOrderDesk checks passed.`

## Brownfield checks

- Managed/native Bug Lab impact completed and returned 5 direct callers, 2 transitive callers, and 3 native candidates. It correctly reported incomplete analysis because 2 delegate targets remain unresolved; this is visible evidence, not an inferred edge.
- Semantic self-inspection completed with 12/12 managed projects loaded, 11,016 resolved invocations, 7 resolved delegate targets, 12 unresolved delegate targets, 0 workspace issues, and 0 compiler errors.

## Controlled comparison

The three counterbalanced baseline/assisted Codex pairs met the frozen gate: pooled recall was 50% versus 100%, precision was 100% in both conditions, all 3 pairs favored BrownPants, and all 6 post-fix checks passed. The result is specific to the delegate persistence fixture. Raw input snapshots, model outputs, JSONL events, token usage, post-fix source files, and check logs are archived under `examples/brownfield-bug-lab/evaluation/delegate-impact/runs/codex-cli-0.162.1-gpt-6.1-sol-20261015/`.

## PR and CI status

- The maintainer approved the changed acceptance criteria in chat, and PR #1 carries the `acceptance-criteria-reviewed` label.
- PR #1 is open as a draft: https://github.com/sidtrey/BrownPants/pull/1. It has not been merged or approved by the author.
- GitHub Actions run 105 passed on Windows, Ubuntu, and macOS for head commit `b8831f8c09dc1ac9c9429df53681da9c3c48c0dd`.
- The first CI run exposed a PowerShell native exit-code leak in the acceptance-review checker. Commit `b8831f8` resets the exit code after successful validation; the policy regression test and all 299 .NET tests pass locally, and the subsequent three-platform CI run passed.

Human PR review and merge remain outstanding release gates.
