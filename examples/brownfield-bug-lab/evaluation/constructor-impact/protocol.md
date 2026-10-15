# Constructor Impact Comparison Protocol

This evaluation compares one Codex run without BrownPants evidence (baseline) with one run given BrownPants `impact.md` and `evidence.json` (assisted). It uses an isolated copy of the same .NET 10 order desk, one frozen task prompt, one explicit model, and the same `workspace-write` sandbox. The hidden answer key is consumed only by the scorer.

## Reproduce

Run from the repository root in PowerShell with .NET 10, `pwsh`, and an authenticated Codex CLI:

```powershell
pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath examples/brownfield-bug-lab/evaluation/constructor-impact/task-spec.md
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/constructor-impact/Prepare-ConstructorComparison.ps1 -DemoRoot examples/brownfield-bug-lab -OutputRoot "$env:TEMP/brownfield-constructor-run"
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/constructor-impact/Run-ConstructorComparison.ps1 -RunRoot "$env:TEMP/brownfield-constructor-run" -Model gpt-6.1-sol
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/constructor-impact/Score-ConstructorComparison.ps1 -RunRoot "$env:TEMP/brownfield-constructor-run"
```

The preparation step verifies both clean checks pass, both seeded checks fail with the expected message, and the seeded sources match. The runner executes baseline first and assisted second, preserving Codex JSONL events, final JSON responses, usage, and independent postcheck logs under the run directory. Do not expose `answer-key.json` or scorer details to agents.

## Scoring and limits

The scorer matches path, relationship category, and signatures against a frozen source-derived key. It normalizes workspace-relative versus project-root-relative paths and equivalent constructor spellings (`TypeName(...)` and Roslyn `.ctor(...)`); test identification is file-level, so line and status are not compared. Duplicates and decoys count as false positives. “Architecture candidate” means static evidence, not proof of runtime DI activation.

The predeclared advantage rule requires at least a 20 percentage-point gain in actionable-impact recall with no precision loss, and both agents must produce a correct, passing fix. A single pair is a demonstration, not general evidence. Token counts and elapsed times are observations for these runs; execution order and caching can affect them. An initial harness attempt with a schema rejected by Codex strict JSON mode was excluded before agent work; the corrected schema was identical in both measured conditions.
