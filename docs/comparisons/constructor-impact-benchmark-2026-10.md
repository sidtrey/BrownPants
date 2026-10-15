# Constructor Impact Benchmark (October 2026)

## What this measures

This one-pair comparison uses the same seeded constructor-injection bug, task, model (`gpt-6.1-sol`), sandbox, and independent .NET check in both runs. The assisted run also received BrownPants impact and evidence reports. The bug discards an injected generic repository and constructs a new one, causing the single-item import check to fail.

Both runs made the same correct one-file repair and passed the independent check. BrownPants did not improve the fix itself or managed-call/test recall in this pair. It did help the assisted run identify the two DI registration/resolution impact candidates that baseline missed. The assisted run also submitted three additional architecture candidates that scored as false positives.

| Measure | Baseline | BrownPants assisted |
| --- | ---: | ---: |
| Actionable findings recalled | 8/10 (80%) | 10/10 (100%) |
| Actionable finding precision | 47.06% | 52.63% |
| Managed-call recall | 7/7 (100%) | 7/7 (100%) |
| Architecture candidates recalled | 0/2 | 2/2 |
| Test-path candidates recalled | 1/1 | 1/1 |
| Correct fix and passing check | Yes | Yes |
| Input tokens (cached subset) | 417,949 (384,256) | 182,177 (149,120) |
| Output tokens (reasoning subset) | 5,880 (1,367) | 4,188 (369) |
| Elapsed time | 199.82 s | 112.15 s |

The predeclared evidence threshold was a 20 percentage-point recall gain with no precision loss, provided both fixes passed. This pair met it exactly: recall increased by 20 points and precision increased by 5.57 points. The change came from architecture candidates; managed-call and test recall were already complete in baseline. These token and time measurements describe this pair only. The baseline ran first, so cache and order effects may matter.

## Reading the result

The evidence supports a narrow brownfield benefit: in this scenario, the BrownPants handoff exposed DI-related impact candidates alongside call impact, giving the agent more context about where an injected dependency was wired. It does not establish that BrownPants generally fixes bugs better, and static candidates do not prove runtime container behavior. One paired run is a demonstration, not a general benchmark claim.

Scoring normalizes project-relative path roots and equivalent constructor signature spellings (`TypeName(...)` versus Roslyn `.ctor(...)`). Test-path identification is file-level. An initial harness schema was rejected by Codex strict JSON validation before any agent work; it was excluded, and the corrected schema was used identically in both measured runs. See the [reproduction protocol](../../examples/brownfield-bug-lab/evaluation/constructor-impact/protocol.md) and [machine-readable results](constructor-impact-benchmark-2026-10.json).
