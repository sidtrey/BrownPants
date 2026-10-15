# Implementation Plan: Constructor Impact Bug Benchmark

## Summary

Add a second, independently reproducible Codex CLI comparison to the existing Brownfield Bug Lab. It seeds a broken generic constructor injection, provides constructor-aware BrownPants impact evidence only to the assisted copy, scores source repair and impact findings independently, and records the actual run without overwriting the batch-bug comparison.

## Technical Context

- BrownPants and the demo test application use .NET 10; legacy .NET Framework, MFC/C++/CLI, and NHibernate files remain evidence-only.
- Existing PowerShell evaluation utilities create isolated copies and score structured findings; the new scenario has its own inputs and result files.
- `codex exec` is the benchmark runner, invoked with one explicitly selected model and `workspace-write` sandboxing rooted at each temporary source copy.
- No new NuGet dependencies or changes to BrownPants runtime code are needed.

## Repository Evidence

- `examples/brownfield-bug-lab/managed/Workflows/OrderImportWorkflow.cs` contains the injected repository constructor and generic workflow.
- `examples/brownfield-bug-lab/managed/tests/Program.cs` checks single-item, batch, and mapped persistence through explicitly constructed workflows.
- `examples/brownfield-bug-lab/managed/Workflows/OrderImportWorkflow.cs` also contains target-typed factory construction and the local DI registration route.
- `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs` follows canonical generic target identity across constructor and method edges.
- `examples/brownfield-bug-lab/evaluation/Prepare-Comparison.ps1` and `Score-Comparison.ps1` are the existing paired harness; their batch-bug inputs must remain unchanged.

## Design

### Evaluation artifacts

Create `examples/brownfield-bug-lab/evaluation/constructor-impact/` with a frozen `task-spec.md`, hashed acceptance criteria, bug report, prompt, result schema, hidden answer key, seed script, preparation script, Codex runner, scorer, and protocol. Keep the answer key and scorer outside both agent working directories.

The preparation command copies the same managed and legacy files to `baseline-source` and `assisted-source`, changes exactly the workflow constructor assignment in each, and verifies equal hashes. It runs the clean fixture check before seeding and requires the seeded check to fail with the expected single-item persistence message. It generates `impact.md` from the seeded assisted copy and places it only in that agent directory.

The execution command invokes `codex exec` sequentially against each copy with identical task content, model, CLI options, and sandbox level. It stores JSONL events, last messages, and tool output outside the copies. It records failures as failures and extracts usage only from observed Codex events.

### Scoring and publication

The result schema separates the proposed code fix from source findings. The scorer validates JSON, checks actual post-run source and tests, and computes TP/FP/FN and precision/recall for the expected constructor calls, caller paths, test candidates, and decoys. Publish one machine-readable result and one Markdown case study. Report token use and one-pair limitations; apply the frozen 20pp/no-precision-loss criterion without tuning it after the run.

## Risks

- Codex may not edit or test the copy, may emit malformed results, or may exceed its configured time: report a failed/incomplete run rather than omit it.
- Token usage fields may be absent or schema-dependent: preserve raw JSONL and report usage as unavailable unless a valid completion event supports it.
- The test-path heuristic is not proof of coverage; scoring treats it as a candidate category.
- No observed result supports a universal productivity or code-quality claim.

## Validation

Verify the task hash, run clean/seeded prechecks, execute one matched pair, score the hidden key, rerun post-fix checks, verify source hashes and condition separation, inspect the raw usage events, and run `git diff --check` plus the acceptance evidence validator.
