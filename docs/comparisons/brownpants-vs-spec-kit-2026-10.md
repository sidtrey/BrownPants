# Spec Kit and Spec Kit + BrownPants: controlled comparison

## Result at a glance

This comparison used BrownPants itself and a source-level regression in its real `RepositoryScanner.IsTestPath` method. Both Codex runs received the same Spec Kit spec, plan, tasks, prompt, source revision, seeded defect, and model. The assisted run additionally received BrownPants repository, investigation, and call-impact evidence; the experiment driver also checked its acceptance hash and evidence ledger.

Both agents restored the same exact `test` / `tests` directory rule. The focused scanner tests passed **75/75** and the complete Release suite passed **235/235** in each independent worktree. In this pair, BrownPants added useful navigation and impact evidence, but the agents produced the same code change and test outcome.

Provider-reported Codex usage was:

- **Spec Kit artifacts alone:** 153,864 input tokens (127,232 cached), 1,047 output tokens.
- **Same artifacts + BrownPants reports:** 186,445 input tokens (163,072 cached), 1,448 output tokens.

The assisted run consumed more tokens after receiving extra reports. This single pair does not establish general token savings, quality differences, productivity differences, or model performance.

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

### Spec Kit artifacts alone

The baseline received the shared feature spec, plan, task list, frozen prompt, source, and seeded failure record. BrownPants reports were not supplied, and its Spec Kit hooks were disabled in this worktree. The agent inspected `RepositoryScanner.IsTestPath` and the existing scanner test, then restored exact directory-component recognition. Its Codex session recorded four completed command-execution events.

### Same Spec Kit artifacts with BrownPants

The assisted run received all of the same task inputs plus three BrownPants reports generated from the seeded worktree:

- `inspect` inventoried **583 files** and identified **69 C# source files**.
- `investigate --clue IsTestPath` returned **10 literal matches**, including `RepositoryScanner.cs`, `RepositoryInvestigator.cs`, and `CallImpactAnalyzer.cs`.
- `impact --changed src/BrownPants.Cli/Analysis/RepositoryScanner.cs --limit 10000` reported complete semantic analysis, **49 direct caller paths**, **138 transitive caller paths**, **20 affected files**, **2 projects**, **12 test-path candidates**, and maximum observed depth **8**. These are file-level impact results across methods in the changed scanner file; the counts do not mean every path calls `IsTestPath` directly or that every candidate is covered by tests.

The assisted agent verified the relevant source and restored the same directory rule. It recorded five completed command-execution events. The experiment driver also verified the task acceptance hash and structurally validated the three-task, six-criterion evidence ledger in the assisted worktree. That ledger check validates IDs, paths, and task mapping; it does not judge test quality.

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

Two preliminary baseline setup attempts ended without a completed turn record and are excluded from the measured pair and token totals. Both measured worktrees were reset to the same commit and seeded hashes before the formal runs.

## Limits

This is one matched pair on one repository, one task, one Codex CLI version, and one model. The run exercised shared Spec Kit artifacts through Codex CLI; it did not benchmark every Spec Kit lifecycle phase or other agent hosts. BrownPants call impact is static evidence, not runtime reachability or test coverage. Equal test results do not prove equal code quality in general, and observed usage counts do not predict future token use.
