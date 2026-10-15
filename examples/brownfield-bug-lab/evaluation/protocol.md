# Evaluation protocol

## Conditions

1. Prepare two copies from the same fixed fixture and apply the same seed to each.
2. Give both agents the same bug report, acceptance criteria, prompt, source tree, model, host, and post-run checks.
3. Give the assisted agent BrownPants inspect/investigate/impact outputs from its seeded copy. Do not copy the answer key or scoring script into either agent worktree.
4. Each agent writes `result.json` using `result.schema.json`; the experiment driver scores after both runs end.

## Scoring

For each condition, calculate precision and recall separately for the four known impact domains: managed calls, architecture candidates, framework candidates, and test identification. A finding is a true positive only when its ID, category, and path match the answer key; managed edges must also identify the correct target signature. Unsupported MFC-to-managed edges count as false positives when asserted as resolved. Excluded decoys are false positives. Publish the counts and denominators, not percentages alone.

BrownPants advantage is demonstrated only if assisted recall for actionable impact findings is at least 20 percentage points higher than baseline and precision is no lower. Otherwise publish the measured result without a superiority claim and revise the task or fixture before advertising it.

This is one controlled scenario and does not establish general performance, cost, or time savings.
