# Implementation Plan: Reproducible Brownfield SDD Comparison

## Summary

Replace the synthetic-looking README comparison with a controlled experiment on actual BrownPants source. Use an isolated failing regression in `RepositoryScanner.IsTestPath`, run identical Spec Kit artifacts through Codex CLI with and without BrownPants evidence, independently review each result, and publish sanitized evidence with exact commands and observed outcomes.

## Technical Context

- **Application**: .NET 10 CLI with xUnit tests.
- **Comparison host**: installed Codex CLI, pinned model and CLI version recorded from observed run metadata.
- **SDD artifacts**: committed feature-local `spec.md`, `plan.md`, `tasks.md`, and acceptance hash shared by both worktrees.
- **BrownPants treatment**: local inventory and relevant source/impact evidence plus the repository's acceptance/evidence workflow; all actual commands are recorded.
- **Experiment isolation**: two temporary detached Git worktrees from one source commit, same seeded source/test patch, and clean per-condition state. Raw JSONL is stored under a temporary local directory and is not committed.
- **Validation**: seeded test must fail before either agent runs; after each run, execute the identical focused and complete Release test commands from the worktree.

## Constitution Check

- **Evidence First**: report direct run output separately from product capability and reviewer interpretation; distinguish syntax candidates and limits.
- **Engine-Neutral Core**: this demo uses Spec Kit as one adapter and does not imply BrownPants requires it.
- **Deterministic and Reviewable Output**: pin common inputs, publish hashes, exact task, diffs, commands, and sanitized outputs.
- **Safe Inspection by Default**: gather default local inventory without network or project evaluation; do not claim calls are semantically resolved unless the recorded command uses `--semantic`.
- **Specified and Verified Changes**: baseline criteria before work, map tasks to evidence, run identical tests and validate the ledger.

## Experiment Design

The shared task is to repair path-based test classification after a controlled regression removes directory-component recognition from the real `RepositoryScanner.IsTestPath` method. The regression test requires `tests/fixtures/invoice.json` to be recognized as a test path while keeping the actual source path `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` out of test candidates; conventional test filenames must remain recognized. The injected source and failing test are applied identically to both detached worktrees before running either agent.

Both conditions use the same new feature artifacts, exact prompt, Codex CLI version, selected model, environment, starting commit, and test commands. The baseline follows the Spec Kit artifacts without invoking BrownPants. The assisted run follows those same artifacts and additionally captures BrownPants inventory and source/impact evidence, then validates the shared acceptance hash/evidence ledger. Record provider-reported completion usage only when observed.

## Deliverables

- A pinned task-specific Spec Kit spec and acceptance hash for the seeded fix.
- A committed seed patch or exact reproducible seed procedure and pre-run failing test evidence.
- A detailed comparison report with per-run inputs, outputs, changed paths, test commands/results, reviewer findings, and limits.
- A concise scenario-based README presentation that does not claim the pair proves general superiority or token savings.

## Risks and Mitigations

- Agent runs are stochastic: publish one-pair limits and preserve both full observed outcomes.
- A test can miss a defect: report human review and any corrections independently from test pass status.
- Tool startup/model metadata may differ: verify against observed session events and invalidate the pair if a shared control differs.
- Prompt or report can expose repository data: keep raw logs temporary, review every published excerpt, and scan the final diff for secrets.
