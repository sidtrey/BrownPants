# Quickstart: Constructor Impact Bug Benchmark

The benchmark writes only beneath a new output directory. It refuses to reuse an existing path. Start with the deterministic preparation gate:

```powershell
$run = Join-Path $env:TEMP 'brownfield-constructor-impact-run'
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/constructor-impact/Prepare-ConstructorComparison.ps1 `
  -DemoRoot (Resolve-Path examples/brownfield-bug-lab) -OutputRoot $run
```

After confirming `preparation.json` reports identical seeded source and expected failing checks, run both Codex conditions with the same explicit model:

```powershell
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/constructor-impact/Run-ConstructorComparison.ps1 `
  -RunRoot $run -Model gpt-6.1-sol
```

Score the captured final responses and independent post-fix checks:

```powershell
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/constructor-impact/Score-ConstructorComparison.ps1 `
  -RunRoot $run
```

The answer key and scorer stay outside agent workspaces. Publish the resulting score even if the assisted run does not pass the predeclared advantage threshold. A single pair is a scenario result, not a general quality or efficiency estimate.
