# Brownfield Bug Lab

This local fixture models an order desk with a package-free .NET 10 core and evidence-only .NET Framework, MFC/C++/CLI, and NHibernate artifacts. The legacy projects are for inspection and are not built.

## Reproduce the fixed demo checks

```powershell
dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release
```

## Run BrownPants analysis

From the repository root, run the checkout's CLI against the entire fixture:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab --format markdown --limit 1000
```

Run semantic call impact from the managed root. This resolves the generic single-item, batch, and mapped calls without loading the evidence-only legacy projects:

```powershell
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab/managed --changed Persistence/OrderRepository.cs --format markdown --limit 1000
```

Trace a changed C++/CLI bridge back through its native caller and MFC command map. To trace a changed managed workflow in the other direction, select its C# file; the typed bridge call and upstream MFC links appear as lexical candidates, separate from Roslyn-resolved managed calls:

```powershell
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab `
  --changed legacy/Bridge/OrderManagedBridge.cpp --format markdown --limit 1000

dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab `
  --changed managed/Workflows/OrderImportWorkflow.cs --format markdown --limit 1000
```

For an SDD agent, create a single JSON handoff containing project context, source-located architecture/framework candidates, and test-path candidates:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab --format json --limit 1000
```

The handoff is task-neutral, so agents should use the bug report to select relevant findings and verify them against source. Candidates remain candidates. Use the separate semantic `impact` report for resolved managed callers.

For a task-focused handoff, supply the bug clue and changed managed file. Selectors combine as a union; every result includes the selector and reason that brought it into the report:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab/managed `
  --clue "OrderImportWorkflow" --changed Workflows/OrderImportWorkflow.cs `
  --semantic --format json --limit 1000
```

This handoff keeps only matching source facts plus resolved direct/transitive callers and related test-path candidates. Check `focus` for unmatched selectors, evidence limits, and omitted counts. Omitting the selectors still produces the complete task-neutral handoff.

The managed impact report connects `ImportBatch<T>` to the `Save(IEnumerable<T>)` overload and shows how `ImportMapped<TSource>` forwards through `SaveMapped<TSource>`. Native impact on the bridge reports `COrderMfcView::OnImportBatch` and the `ID_ORDER_IMPORT_BATCH` message-map entry as candidate paths. A changed managed workflow also traces to the typed C++/CLI handle call and its upstream MFC candidates. BrownPants does not expand macros, prove compiler binding or runtime dispatch, or establish that the evidence-only bridge builds. Inventory also locates factory/DI wiring and NHibernate dependency/session/configuration/mapping evidence; ORM candidates do not prove database behavior.

### Trace a generic factory constructor

To see constructor-aware impact in the same fixture, inspect the workflow file's explicit constructors and callers:

```powershell
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab/managed `
  --changed Workflows/OrderImportWorkflow.cs --format markdown --limit 1000
```

The report resolves the target-typed factory expression `new(repository)` to the closed generic constructor `OrderImportWorkflow<Order>..ctor(OrderRepository<Order>)`, while matching it to the open generic declaration. The observed graph has four direct callers and one transitive caller across two projects; one caller is in the test candidate path. The DI registration is visible in the path, but static source analysis does not prove runtime container activation.

## Reproduce the seeded bug comparison

`evaluation/` contains the frozen task, seed script, answer key, prompt, and evaluator. `Prepare-Comparison.ps1` creates matched copies in a new output directory, injects the same bug into both, captures the expected failing check, and writes BrownPants reports only into the assisted copy. It invokes this repository's CLI project so an older globally installed command cannot change the evidence.

The initial measured pair fixed the same bug and passed the same independent checks. It did **not** meet the predeclared advantage threshold: recall rose five points while precision fell. A follow-up added the structured evidence handoff and met the frozen threshold on the same task; its results and limits are in [`docs/comparisons/brownfield-evidence-handoff-2026-10.md`](../../docs/comparisons/brownfield-evidence-handoff-2026-10.md). The original comparison remains in [`docs/comparisons/brownfield-bug-lab-2026-10.md`](../../docs/comparisons/brownfield-bug-lab-2026-10.md). Run the original evaluator self-check with `-SelfTest` to verify its answer-key facts and decoys.

### Constructor injection comparison

The separate `evaluation/constructor-impact/` scenario seeds a generic workflow bug that ignores its injected repository. Its matched Codex pair made the same correct fix and passed the same independent check. The assisted run found two DI registration/resolution impact candidates the baseline missed, bringing actionable recall from 80% to 100% while precision increased from 47.06% to 52.63%. This met the frozen 20-point recall threshold in this single pair, but did not improve managed-call or test-path recall, and it added three architecture-candidate false positives. Treat it as a narrow demonstration, not a general performance claim. See the [measured result](../../docs/comparisons/constructor-impact-benchmark-2026-10.md) and [reproduction protocol](evaluation/constructor-impact/protocol.md).
