# Data Model: Constructor Impact Bug Benchmark

- **Scenario inputs**: frozen task specification, acceptance hash, bug report, prompt, and result schema.
- **Prepared condition**: name (`baseline` or `assisted`), source-copy path, seeded source digest, seed precheck result, BrownPants evidence availability, and agent execution status.
- **Agent result**: source-located findings, proposed diagnosis/fix, changed paths, limitations, and any malformed-output or execution error.
- **Finding score**: domain, expected count, submitted count, true positives, false positives, false negatives, precision, and recall.
- **Run metadata**: Codex CLI version, selected model, sandbox, source and task digests, pre/post test commands/results, raw event log path, and observed input/output token usage or an explicit unavailable value.
- **Comparison result**: baseline and assisted run records, score deltas, frozen advantage-rule outcome, and sample-size limitation.

The answer key is benchmark-only data and is never copied into either agent worktree. An incomplete agent execution remains in the comparison with a failed status and is not silently removed from denominators.
