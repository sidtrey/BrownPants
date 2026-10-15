# Brownfield Bug Lab: Spec Kit and Spec Kit + BrownPants

## Purpose

This matched scenario tests a bug in a small order import service. The managed code uses generic methods and same-name overloads; adjacent analysis-only artifacts model .NET Framework 4.7.2, MFC/C++/CLI, factory and DI wiring, and NHibernate metadata. The defect skips an order marked `RequiresRetry` in the batch overload. Such an order must be retained for another processing attempt.

The baseline and assisted agents received the same seeded source, task spec, bug report, prompt, result schema, Codex CLI, and model. The assisted copy also received BrownPants inventory, investigation, and semantic impact reports. The answer key and scoring script were kept out of both copies.

## BrownPants evidence from the fixture

The repository's BrownPants CLI reports source-linked candidate evidence for the old framework project, MFC and CLR project settings, C++/CLI bridge, factory and DI sites, NHibernate package and API use, and NHibernate XML mapping/configuration. The inspection report has 21 candidate signals. These are leads for review; they do not prove runtime use.

Semantic impact on `OrderRepository.cs` identifies these managed edges:

```text
ImportOne<T>       -> Save(T)
ImportBatch<T>     -> Save(IEnumerable<T>)
ImportMapped<T>   -> SaveMapped<TSource>(...)
SaveMapped<TSource> -> Save(IEnumerable<T>)
```

The mapped generic path therefore reaches the same batch overload. BrownPants reports four direct callers and one transitive path in this fixture. The MFC bridge body is a stub, so the report does not claim that its handler reaches the managed order workflow. NHibernate session and mapping signals are also candidates, not runtime call or database evidence.

## Paired result

The seeded regression failed in both copies with `Batch import saved 1 of 2 orders.` Both agents removed the retry-row skip from the batch overload; the resulting source file hashes are identical. The comparison driver independently verified the task acceptance hash and ran the same post-fix command in both copies:

```powershell
dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release
```

Both checks passed. Each agent's final message said its own requested hash/test checks were unavailable; the driver reran them after the agent turns and verified the results.

| Finding domain | Expected | Spec Kit hits / submitted | Spec Kit + BrownPants hits / submitted |
| --- | ---: | ---: | ---: |
| Managed call edges | 4 | 4 / 4 | 4 / 4 |
| Factory and DI candidates | 5 | 0 / 0 | 0 / 1 |
| MFC and NHibernate candidates | 10 | 0 / 2 | 2 / 3 |
| Test candidates | 1 | 1 / 1 | 0 / 1 |
| **All findings** | **20** | **5 / 7** | **6 / 9** |

Overall recall was **25%** for Spec Kit and **30%** for Spec Kit + BrownPants, a **5 percentage-point** gain. Precision was **71.4%** and **66.7%**, respectively. The frozen bar was at least 20 points more recall with no precision loss, so this pair **does not demonstrate an overall BrownPants advantage**. The agents reported all four managed call relationships in this pair, so it also does not show better call-edge recall from the assisted agent.

The result still shows what BrownPants contributes as a tool: it generates the source-linked inventory and semantic call map directly, before an agent writes its summary. In this pair, the assisted structured result credited the NHibernate package and MFC message-map candidates; it put the C++/CLI bridge under the wrong finding category and omitted the required status on its test candidate. Factory/DI signals supplied in the report were not carried into the structured result. The baseline's generic MFC/NHibernate boundary notes did not match the exact expected source signals. These output-contract misses are counted as false or missed findings by the frozen evaluator.

## Token use

Codex's completed-turn usage records reported:

| Condition | Input tokens (cached subset) | Output tokens |
| --- | ---: | ---: |
| Spec Kit | 299,072 (269,824 cached) | 3,531 |
| Spec Kit + BrownPants | 389,425 (344,576 cached) | 3,331 |

This run used more input tokens with BrownPants reports. It did not measure time or money and supports no efficiency claim.

## Reproduce

From the repository root, inspect the fixture and resolve its managed impact with the source checkout's CLI:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab --format markdown --limit 1000
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab/managed --changed Persistence/OrderRepository.cs --format markdown --limit 1000
```

Prepare matched, seeded copies in a new directory outside the checkout. The script invokes the repository's CLI project so a stale globally installed tool cannot change the evidence:

```powershell
$run = 'F:\temp\brownfield-bug-lab-run'
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/Prepare-Comparison.ps1 `
  -DemoRoot (Resolve-Path examples/brownfield-bug-lab) -OutputRoot $run
```

Run the same `control/task-prompt.md` in `baseline-source` and `assisted-source` with the same Codex model and host; provide the prepared BrownPants reports only to the assisted copy. After both turns, score the `result.json` files and rerun the post-fix check in each copy:

```powershell
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/Score-Comparison.ps1 `
  -AnswerKeyPath examples/brownfield-bug-lab/evaluation/answer-key.json `
  -BaselineResultPath "$run/baseline-source/result.json" `
  -AssistedResultPath "$run/assisted-source/result.json" `
  -OutputPath "$run/comparison.json"
```

Run the evaluator's ground-truth and decoy self-check by adding `-SelfTest`. The committed machine-readable results record this completed pair, including task and prompt hashes, source hashes, test outcomes, per-domain scores, and token usage: [`brownfield-bug-lab-2026-10-results.json`](brownfield-bug-lab-2026-10-results.json).

## Limits

This is one scenario, one paired run, one model, and one Codex CLI version. A repeated run may produce different summaries. The project does not compile its MFC or .NET Framework evidence, prove NHibernate runtime behavior, measure test coverage, or establish general quality, time, or cost improvements. The comparison remains explicit about its negative overall result rather than presenting it as a superiority demo.
