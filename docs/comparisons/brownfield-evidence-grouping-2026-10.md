# Brownfield Evidence Grouping Comparison

## What was compared

The same frozen batch-save bug task was run in two copies of the same Brownfield Bug Lab fixture with Codex CLI 0.162.1 and `gpt-6-luna`. The baseline received no BrownPants reports. The assisted copy received BrownPants inspection, investigation, structured evidence, and impact reports. Both received the same final v4 task prompt and result format. The answer key was reviewed against fixture evidence at `cc91a0376975ac52b64b5fc7c33417afaf5a2736`; its SHA-256 and the task acceptance hash are recorded in the [machine-readable results](brownfield-evidence-grouping-2026-10-results.json).

Both seeded prechecks failed with `Batch import saved 1 of 2 orders.` Both agents removed the retry-row skip in `OrderRepository<T>.Save(IEnumerable<T>)`, produced identical fixed-source SHA-256 values, and passed `dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release`.

## Results

| Measure | Spec Kit | Spec Kit + BrownPants |
| --- | ---: | ---: |
| Expected findings | 20 | 20 |
| True positives | 20 | 20 |
| False positives | 3 | 3 |
| False negatives | 0 | 0 |
| Precision | 86.96% | 86.96% |
| Recall | 100% | 100% |
| Provider input tokens (cached subset) | 704,193 (635,904) | 325,087 (290,304) |
| Provider output tokens | 7,379 | 5,010 |

The assisted result exceeds the earlier reported 74.1% precision while retaining 100% recall, but the baseline reached the same final score. This pair therefore shows **no measured improvement in finding quality** from BrownPants. It does show that the structured handoff supplied the same source-linked groups with substantially fewer recorded input tokens in this one run; token totals do not establish cost or time savings, and this single pair is not enough to attribute the difference to BrownPants. The exact emitted group locations and finding IDs are retained in [`grouped-candidate-evidence-v4.json`](../../examples/brownfield-bug-lab/evaluation/grouped-candidate-evidence-v4.json).

The three false positives in each result were the NHibernate session-factory group, the MFC dynamic-creation marker in `OrderMfcView.h`, and the legacy .NET Framework target project. The scorer rejects grouped candidate reports if their `lines` differ from the reviewed key; its self-test checks both an exact match and an unrelated extra line.

Re-run the checked-in score and self-test from the repository root:

```powershell
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/Score-Comparison.ps1 `
  -AnswerKeyPath examples/brownfield-bug-lab/evaluation/answer-key-v4.json `
  -BaselineResultPath examples/brownfield-bug-lab/evaluation/baseline-result-v4.json `
  -AssistedResultPath examples/brownfield-bug-lab/evaluation/brownpants-result-v4.json `
  -OutputPath examples/brownfield-bug-lab/evaluation/comparison-v4.json

pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/Score-Comparison.ps1 `
  -AnswerKeyPath examples/brownfield-bug-lab/evaluation/answer-key-v4.json `
  -BaselineResultPath NUL -AssistedResultPath NUL -OutputPath NUL -SelfTest
```

## Prompt refinement and limits

The first v4 prompt iteration is retained in [`task-prompt-v4-pilot.md`](../../examples/brownfield-bug-lab/evaluation/task-prompt-v4-pilot.md) and included in the results artifact. In that pilot, the assisted agent did not copy the exact semantic call signatures and included test callers as managed-call findings; it scored 16/20 (80% recall, 64% precision), versus 6/20 (30% recall, 60% precision) for baseline. The prompt was then clarified to require verbatim direct-call rows from `impact.md`, and both conditions were rerun. Since this clarification followed review of the pilot, the final pair is prompt-development evidence rather than an independent confirmation of a BrownPants advantage.

The earlier evidence-handoff report referenced source commit `83c3f46177b5b66ff1e0e79acd0811d18d2b1a92`, which is unavailable in the local checkout and public Git history; its C++/CLI answer-key location also does not match the current fixture. Its v3 files are preserved, but that score is not used as the v4 baseline. The v4 answer key and hash are separate. The 20 expected facts and frozen task acceptance hash `777ecb0e7523c67dc4a3e8b28cebae70e7b9476939c00a65eea50308c7d3dd63` remain unchanged.

This is one task, model, and host. MFC, C++/CLI, .NET Framework, and NHibernate evidence remains candidate evidence; the fixture does not establish their runtime relationship to the managed defect. The machine-readable results preserve both final agent responses, token usage, per-category scores, hashes, and the prompt-refinement pilot.
