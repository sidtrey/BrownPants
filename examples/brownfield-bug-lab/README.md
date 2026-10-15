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

For an SDD agent, create a single JSON handoff containing project context, source-located architecture/framework candidates, and test-path candidates:

```powershell
dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab --format json --limit 1000
```

The handoff is task-neutral, so agents should use the bug report to select relevant findings and verify them against source. Candidates remain candidates. Use the separate semantic `impact` report for resolved managed callers.

The impact report connects `ImportBatch<T>` to the `Save(IEnumerable<T>)` overload and shows how `ImportMapped<TSource>` forwards through `SaveMapped<TSource>`. Inventory also locates factory/DI wiring, MFC message-map and runtime-type markers, a C++/CLI bridge, and NHibernate dependency/session/configuration/mapping evidence. Those native and ORM findings are candidates; this fixture does not prove runtime relationships or database behavior.

### Trace a generic factory constructor

To see constructor-aware impact in the same fixture, inspect the workflow file's explicit constructors and callers:

```powershell
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab/managed `
  --changed Workflows/OrderImportWorkflow.cs --format markdown --limit 1000
```

The report resolves the target-typed factory expression `new(repository)` to the closed generic constructor `OrderImportWorkflow<Order>..ctor(OrderRepository<Order>)`, while matching it to the open generic declaration. The observed graph has four direct callers and one transitive caller across two projects; one caller is in the test candidate path. The DI registration is visible in the path, but static source analysis does not prove runtime container activation.

## Reproduce the seeded bug comparison

`evaluation/` contains the frozen task, seed script, answer key, prompt, and evaluator. `Prepare-Comparison.ps1` creates matched copies in a new output directory, injects the same bug into both, captures the expected failing check, and writes BrownPants reports only into the assisted copy. It invokes this repository's CLI project so an older globally installed command cannot change the evidence.

The measured pair fixed the same bug and passed the same independent checks. It did **not** meet the predeclared overall advantage threshold: recall rose five points while precision fell. BrownPants' direct inventory and call map provide the brownfield evidence this fixture is designed to show; the agent head-to-head is reported with its limitations in [`docs/comparisons/brownfield-bug-lab-2026-10.md`](../../docs/comparisons/brownfield-bug-lab-2026-10.md). Run the evaluator self-check with `-SelfTest` to verify all answer-key facts and ensure both decoys are rejected.
