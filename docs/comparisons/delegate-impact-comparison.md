# Delegate Impact Comparison

This controlled evaluation met the pre-registered criterion for improved managed-impact recall on this delegate fixture.

- Model / CLI: gpt-6.1-sol / codex-cli 0.162.1
- Sandbox: workspace-write
- Task acceptance SHA-256: 3d5469a244e1a08c602e01b140af98da5c89d165b83e89b201a00493565032f3
- Answer-key SHA-256: eb212b96228435f94b0d9d4396964d5eb8a6b0c48ebf79d65e3560d5ab0a9fc0
- Matched seeded source SHA-256: 59d53be2fe46a27140a1c0065f1030481eb746d4205dfae4dd436ebacf101c29
- Pooled baseline recall / precision: 0.5 / 1
- Pooled BrownPants recall / precision: 1 / 1
- Recall gain: 50 percentage points
- Pairs favoring BrownPants: 3 of 3
- All six repairs passed independent checks: True
- Gate passed: True

## Pair Results

| Pair | Baseline recall | BrownPants recall | Gain |
|---:|---:|---:|---:|
| 1 | 0.5 | 1 | 50.0 pp |
| 2 | 0.5 | 1 | 50.0 pp |
| 3 | 0.5 | 1 | 50.0 pp |

## Codex Token Usage

Counts below are the Codex-reported input/output tokens for each task run; they are descriptive and are not part of the success gate.

| Pair | Baseline input / output | BrownPants input / output |
|---:|---:|---:|
| 1 | 129865 / 738 | 108214 / 744 |
| 2 | 107075 / 707 | 107766 / 763 |
| 3 | 129787 / 745 | 110606 / 729 |

## Limitations

- Static call impact is source evidence and does not establish runtime execution.
- Only the seeded single-item persistence defect is evaluated.
- Results apply to the recorded Codex CLI/model/sandbox and this frozen fixture.

Raw Codex JSONL, structured outputs, usage records, original seeded input snapshots, post-fix source files, and independent check logs are retained in the run archive.

## Scoring normalization

- Paths normalize slash direction and a leading `./`.
- Signatures drop `global::` / `global.` and the fixture namespace prefix.
- `delegate invocation` and `delegate-reference` normalize to `delegate`.
- Duplicate and unmatched findings count as false positives.

Artifacts: [scored machine-readable result](delegate-impact-comparison.json), [frozen evaluation inputs and scripts](../../examples/brownfield-bug-lab/evaluation/delegate-impact/), and [raw six-run archive](../../examples/brownfield-bug-lab/evaluation/delegate-impact/runs/codex-cli-0.162.1-gpt-6.1-sol-20261015/).
