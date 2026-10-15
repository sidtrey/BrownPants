# Spec Kit and Spec Kit + BrownPants: controlled comparison

## Why BrownPants matters in this task

The agents made the same fix and passed the same tests. This pair does not show that BrownPants made the code change better. The brownfield value is that the assisted workflow put a source-linked impact map in front of the developer before editing.

The changed classifier has three source-confirmed reporting consumers:

```text
RepositoryScanner.IsTestPath
  ├─ RepositoryScanner.Scan                 → builds Inventory.TestPaths
  ├─ RepositoryInvestigator.Investigate      → labels clue matches as test candidates
  └─ CallImpactAnalyzer.Analyze              → labels impacted call paths as test candidates
```

Evidence locations:

- [`RepositoryScanner.Scan`](../../src/BrownPants.Cli/Analysis/RepositoryScanner.cs#L49) calls `IsTestPath` to build the inventory's `TestPaths`; the classifier's exact directory and filename rules are at [lines 128-134](../../src/BrownPants.Cli/Analysis/RepositoryScanner.cs#L128).
- [`RepositoryInvestigator.Investigate`](../../src/BrownPants.Cli/Analysis/RepositoryInvestigator.cs#L21) uses `IsTestPath` when attaching test-candidate labels to bug-clue matches.
- [`CallImpactAnalyzer.Analyze`](../../src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs#L111) resolves callers and uses `IsTestPath` to label the paths it reports.
- Existing tests demonstrate the user-visible labels in [`RepositoryScannerTests`](../../tests/BrownPants.Tests/RepositoryScannerTests.cs#L151) and [`CallImpactAnalyzerTests`](../../tests/BrownPants.Tests/CallImpactAnalyzerTests.cs#L12).

For a team, this turns “restore test-directory recognition” into a more useful review question: could this change affect the inventory, bug-triage labels, or QA's view of impacted callers? BrownPants surfaces those connected paths and candidate tests so the developer can inspect them and QA can prioritize verification. It does not prove those tests cover the behavior.

## Result at a glance

**Spec Kit alone:** The agent inspected the source and restored the exact `test` / `tests` directory rule. It produced the correct narrow fix for the task.

**Spec Kit + BrownPants:** The agent received the same task plus an inventory, investigation leads, and semantic impact report. The report surfaced the classifier's use in inventory, investigation, and call-impact output before implementation. The agent independently made the same code change.

Both formal runs completed. The expected seeded regression failed before either agent ran. After the agents finished, the experiment driver recorded **75/75 focused tests** and **235/235 full Release tests** in each worktree. Neither agent was asked to run tests; test results were collected independently after both sessions.

Provider-reported usage was **153,864 input tokens** (127,232 cached), **1,047 output** for Spec Kit alone and **186,445 input** (163,072 cached), **1,448 output** for the assisted run. Extra BrownPants reports increased token use in this pair. The measured benefit demonstrated here is impact visibility, not token savings or improved code quality.

## Shared task and seeded defect

The task was to restore test-path recognition when a test file lives under an exact `test` or `tests` directory. The classifier already supported that behavior in the source revision; the experiment driver deliberately removed the directory-component check in both copies and added the same regression assertions to `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

The seeded behavior was:

```csharp
var parts = file.Split('/');
return IsTestFileName(Path.GetFileName(file));
```

The frozen task required all three outcomes:

1. `tests/fixtures/invoice.json` is a test-path candidate because `tests` is an exact path component.
2. `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` is not a test-path candidate.
3. `src/InvoiceTests.cs` remains a test-path candidate under existing filename rules.

Before either agent ran, the same focused command failed in both worktrees with one failure: `tests/fixtures/invoice.json` was absent from `TestPaths`. Both runs started with the same seeded source hash `38947D3259846093B09C3F5E67ABF0C1505E8273F77C2C1607C96CC429FBAFC5` and regression-test hash `3939A3F8A50668AF5DFCB6B30890730461BC989F7A8531FED483DDCF731F01DC`.

Both agents restored the directory-component branch:

```csharp
var parts = file.Split('/');
return parts.Any(part => part.Equals("test", StringComparison.OrdinalIgnoreCase)
        || part.Equals("tests", StringComparison.OrdinalIgnoreCase))
    || IsTestFileName(Path.GetFileName(file));
```

The regression test was prepared before either run, so neither agent changed the test file. Each agent changed only `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` relative to its seeded starting state.

## What each workflow supplied

The BrownPants evidence was generated with these repository commands:

```powershell
brownpants inspect . --format markdown --limit 10
brownpants investigate . --clue IsTestPath --format markdown --limit 1000
brownpants impact . --changed src/BrownPants.Cli/Analysis/RepositoryScanner.cs --format markdown --limit 10000
```

The figures below are from captured reports in the seeded experiment worktree. Re-running them against a later checkout can produce different counts as the repository changes.

### Spec Kit artifacts alone

The baseline received the shared feature spec, plan, task list, frozen prompt, source, and seeded failure record. BrownPants reports were not supplied, and its Spec Kit hooks were disabled in this worktree. The agent inspected the scanner and existing tests, then restored exact directory-component recognition. Its completed Codex turn recorded four command-execution events.

### Same Spec Kit task with BrownPants

The assisted condition received the same inputs plus three BrownPants reports generated from the seeded worktree: an inventory (583 files, 69 C# sources), literal investigation leads (`IsTestPath` had 10 matches at that time), and a semantic impact report for the changed scanner file. The report summarized 49 direct and 138 transitive caller paths across the file, 20 files, 2 projects, 12 test-path candidates, and maximum observed depth 8. These file-level counts do not mean every path calls `IsTestPath`; selected exact consumers are identified by source line in the evidence section above.

The assisted agent verified the relevant source and restored the same directory rule. Its completed Codex turn recorded five command-execution events. The experiment driver also verified the task acceptance hash and structurally validated the three-task, six-criterion evidence ledger. That ledger check validates IDs, paths, and task mapping; it does not judge test quality.

## Validation results

The experiment driver ran the same commands after both agents completed. The test commands build the relevant project as part of execution.

**Spec Kit artifacts alone:** The focused `RepositoryScannerTests` run passed **75 tests**; the full Release solution suite passed **235 tests**. Both commands reported zero failures.

**Spec Kit + BrownPants:** The focused `RepositoryScannerTests` run passed **75 tests**; the full Release solution suite passed **235 tests**. Both commands reported zero failures.

Neither agent was asked to run tests. The experiment driver captured the identical failing pre-run check, then ran these post-change commands independently in each worktree.

## Inputs and reproducibility

- **Source commit:** `aaadebfed1d48b6f79babe91e4d4f1418a0e2905` (`Lead README with brownfield value`).
- **Host:** Codex CLI `0.162.1`, model `gpt-6-luna`, Windows, PowerShell `7.6.6`, .NET 10 SDK.
- **Shared comparison spec hash:** `7c3da79a20458f759f68b18b5ef87eb6077b3323d2de1fdc15a0a877b497c407`.
- **Shared bug-fix task acceptance hash:** `ee166e7921fa8163d5744acdea4d0e0f2a556f680c96e93edb4b3e05831e61cb`.
- **Shared task spec hash:** `281BC235364E5D5025BEA0AD8EF2BB80CCCD7CBD5A0F6C666E69795C8EB5E1FC`.
- **Shared plan hash:** `F0FA18A3B07A2AB1D3681A12B91FB037B50293060E1511AA248E353937588D1E`.
- **Shared task-list hash at run start:** `E1847E76C924A6DF6982736D7BDF38CFC5CC99600B0B907D55B4E6A6337F23F8`.
- **Shared prompt hash:** `6494CF0EBAA90EDB83ABAA7C895A75FCE62FE72A2A8759676555431450025108`.
- **BrownPants report SHA-256:** inventory `40376E950CC2AF13FA4F4C6DB3FB23166BDBD174F6FB4340B3C4071B29FCAF53E`; investigation `D980442A628F357F8666F5D60209A65FDAB1A00C1647A8660C62159870D96BAC`; impact `800F1CBAC31D32A25093AF95F97C7EF8CD50FC02242818169ACC9D04C44DB1C2`.

The task spec, plan, task list, and prompt are checked in under [`specs/046-brownfield-sdd-comparison/experiment/`](../../specs/046-brownfield-sdd-comparison/experiment/). The same prompt was piped to Codex CLI from each isolated worktree. Provider usage totals above come from each completed Codex JSONL `turn.completed.usage` record; they are not estimates. Raw session logs and temporary worktrees are not included in this repository.

Run-history note: two preliminary baseline setup attempts ended without completed turn records. They were excluded from the measured pair and token totals. Both formal worktrees were reset to the same source commit and seeded hashes; the measured baseline and assisted runs each completed and passed the recorded final tests. The seeded pre-run test failure was expected and confirms the injected regression.

## Limits

This is one matched pair on one repository, one task, one Codex CLI version, and one model. The run exercised shared Spec Kit artifacts through Codex CLI; it did not benchmark every Spec Kit lifecycle phase or other agent hosts. BrownPants call impact is static evidence, not runtime reachability or test coverage. Equal test results do not prove equal code quality in general, and observed usage counts do not predict future token use.
