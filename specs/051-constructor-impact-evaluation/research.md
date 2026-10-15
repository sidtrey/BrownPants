# Research: Constructor Impact Bug Benchmark

## Current evidence

- The clean demo check passes with `dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release`.
- In an isolated temporary copy, replacing the injected-repository assignment in `managed/Workflows/OrderImportWorkflow.cs` with `new OrderRepository<T>()` caused the check to fail at the single-item repository assertion. This is a behaviorally observable constructor defect.
- The packed constructor-aware CLI reports a closed generic construction from `OrderBatchFactory.Create`, two explicit workflow construction sites in `tests/Program.cs`, and a DI registration route. Its impact output separates method and constructor relationships and identifies the test path as a candidate.
- The existing batch-save comparison and result artifacts are frozen and must not be rewritten for this second scenario.
- `codex exec` 0.162.1 is installed and reports an authenticated ChatGPT login. Runs must still be treated as fallible; the harness must record incomplete outputs instead of fabricating a score.

## Decisions

1. **Use a separate scenario directory.** Put the new task, seed, preparation, prompt, schema, scorer, and hidden key under `examples/brownfield-bug-lab/evaluation/constructor-impact/`. Preserve all existing evaluation inputs.
2. **Seed a constructor-injection defect.** Change only the constructor assignment in each copied `OrderImportWorkflow.cs`. Validate clean and seeded checks before any agent run.
3. **Give only the assisted condition BrownPants impact evidence.** Keep the same source, bug report, task specification, prompt, model, CLI version, sandbox, and test commands otherwise.
4. **Separate scoring dimensions.** Verify the actual source repair and independent tests separately from source-located constructor/caller findings. Score exact paths, lines, relation kinds, and signatures against the hidden key; count duplicates and unsupported runtime claims as false positives.
5. **Record one pair first.** Preserve each Codex JSON event log and final response outside the agent worktrees. Extract model and usage from observed events. One pair is a demonstration, not a general benchmark.
6. **Apply the frozen outcome rule.** Claim an advantage only for at least 20 percentage points more actionable-impact recall and no precision loss. Publish every completed outcome, including a tie or loss.

## Rejected alternatives

- Do not reuse or mutate the old batch-bug task: that would mix bug types and invalidate its existing results.
- Do not count a correct fix as evidence BrownPants caused a better fix; the prior paired run showed that both conditions can produce the same patch.
- Do not infer runtime DI activation or causal paths through MFC, C++/CLI, or NHibernate from source markers.
