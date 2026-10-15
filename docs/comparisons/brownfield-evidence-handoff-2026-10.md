# Brownfield Evidence Handoff: Repeat Comparison

## Why this run exists

The first published bug-lab comparison scored BrownPants-assisted findings worse on precision and only five percentage points higher on recall. Investigation showed that agents had to translate broad Markdown reports manually into a structured result. The new `brownpants evidence` command emits source-located, typed JSON; the assisted worktree receives that report alongside the semantic impact report. The revised prompt also states the evidence categories required by the task acceptance dimensions and requires copying BrownPants labels and locations exactly.

The baseline and assisted conditions used the same source, seed, task spec, prompt, result schema, model, CLI, and post-fix check. The evidence files were the only planned condition difference. The answer key and scorer were kept out of both worktrees.

## Results

| Domain | Expected | Baseline TP / submitted | Assisted TP / submitted |
| --- | ---: | ---: | ---: |
| Managed calls | 4 | 4 / 4 | 4 / 4 |
| Factory and DI candidates | 5 | 0 / 2 | 5 / 5 |
| Framework candidates | 10 | 0 / 4 | 10 / 17 |
| Test identification | 1 | 1 / 1 | 1 / 1 |
| **All findings** | **20** | **5 / 11** | **20 / 27** |

The baseline scored **25% recall** and **45.5% precision**. The assisted run scored **100% recall** and **74.1% precision**, a **75 percentage-point recall gain** with higher precision. This pair passes the frozen comparison rule and the 45% recall / 71.4% precision targets for the evidence-handoff feature.

The assisted run submitted seven false positives, all in the framework-candidate domain. They were extra framework signals or duplicate lines beyond the reviewed key. The generator keeps candidate signals explicit; the next quality improvement should group repeated same-signal locations or help agents avoid redundant findings without suppressing distinct evidence.

Both seeded prechecks failed with `Batch import saved 1 of 2 orders.` Both agents made the same repository fix (identical SHA-256), and the independent post-fix check printed `LegacyOrderDesk checks passed.` The task acceptance hash remained `777ecb0e7523c67dc4a3e8b28cebae70e7b9476939c00a65eea50308c7d3dd63`.

## Token use

| Condition | Input tokens (cached subset) | Output tokens |
| --- | ---: | ---: |
| Baseline | 195,768 (167,680 cached) | 4,124 |
| Assisted | 197,225 (158,720 cached) | 4,688 |

The assisted run used 1,457 more input tokens and 564 more output tokens. No time or monetary savings were measured.

## Reproduction and limits

Prepare another isolated pair with the same task prompt:

```powershell
$run = 'F:\temp\brownfield-bug-lab-run'
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/Prepare-Comparison.ps1 `
  -DemoRoot (Resolve-Path examples/brownfield-bug-lab) `
  -OutputRoot $run `
  -TaskPromptPath (Resolve-Path examples/brownfield-bug-lab/evaluation/task-prompt-v3.md)
```

The run's exact result objects, scorer output, host details, hashes, and token counts are in [`brownfield-evidence-handoff-2026-10-results.json`](brownfield-evidence-handoff-2026-10-results.json).

This is one repeated task on one fixture, model, and host. It does not establish general quality, cost, or time improvements across codebases. MFC, C++/CLI, legacy .NET Framework, and NHibernate findings remain candidate evidence; this comparison does not prove their runtime relationship to the defect.
