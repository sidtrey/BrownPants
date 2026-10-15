# BrownPants and vanilla SDD: controlled bug comparison

## Setup

Both conditions used a detached worktree at commit `7a3950825dce2db0b88b951480942d6c52485813`, the same untracked `src/ContestController.cs` fixture, identical task and validation commands, and acceptance criteria SHA-256 `ee43ce2376df97ca44a3bdb497aef2b23cadb91206356a1fa9aa9fdcf7599296`. The feature specification is separately pinned at `569f0f6baa65f8c3827716609c68fd1b208132b8843ae13ec76e424307793077`.

The only planned treatment difference was the context given to the agent. Vanilla received repository source and tests. BrownPants-assisted received a bounded Markdown inventory from `brownpants inspect`; it was asked to verify any leads against source. Both used Codex CLI `0.162.1`, model `gpt-6-luna`, on Windows with PowerShell 7.6.6 and the .NET 10 SDK. The fixture was not committed.

## Successful matched pair

Both runs identified the filename substring check as the cause and added a regression test for `ContestController.cs`, `ContestTests.cs`, and a test-directory path. Vanilla changed the scanner to match `Test`/`Tests` suffixes. BrownPants-assisted also matched `Test` prefixes. Each changed the scanner and one test file; neither committed.

| Measure | Vanilla SDD | BrownPants-assisted |
|---|---:|---:|
| Provider-reported input tokens | 336,632 (297,472 cached) | 260,164 (237,056 cached) |
| Provider-reported output tokens | 2,688 | 2,234 |
| Agent shell invocations | 9 | 8 |
| Sandbox-blocked invocations | 2 | 4 |
| Independent focused test run | 75 passed | 75 passed |
| Independent full Release test run | 229 passed | 229 passed |

Token totals come from each run's `turn.completed.usage.input_tokens` and `output_tokens`, parsed from Codex JSONL. The test runs were repeated by the maintainer outside the agent sandbox because its network policy blocked NuGet/telemetry access. The same restore, focused test, and full test commands were used in both worktrees. There were no code revision cycles in either agent run; both produced one uncommitted draft.

Human review found a defect in both drafts: case-insensitive suffix checks also match ordinary names such as `Latest.cs`; the assisted prefix check also matches `Testimonial.cs`. The shipped scanner uses test-name boundaries and regression cases for both ordinary names. This review is part of the result: passing the requested test suite did not establish that the proposed naming rule was sound.

## Earlier incomplete attempts

The first attempted pair completed the vanilla condition (646,942 input, 4,409 output tokens) but the assisted condition stopped before repository inspection with `orchestrator_helper_incomplete` (121,284 input, 475 output tokens). A later baseline-only attempt was interrupted after sandbox network blocks and had no completed-turn usage event; its counts are unavailable. Neither attempt is part of the matched result above. They are listed so failed attempts are not silently presented as successful comparisons.

## What this does and does not show

This one successful pair shows that BrownPants supplied useful repository context and the assisted agent proposed broader test-name matching. It does not show a reliable token reduction, superior code quality, or general model performance. Token usage and agent behavior vary between runs; more paired runs and other hosts would be needed for those conclusions.

In plain English, the acceptance hash is a fingerprint of the agreed pass/fail conditions. If someone changes those conditions during the task, the fingerprint changes and signals that the change needs review. It helps keep the goalposts visible; it does not prove the code is correct.
