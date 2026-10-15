# Implementation Plan: Brownfield Value Demonstration

## Summary

Rewrite the README faceoff and comparison report so the observed value is easy to see: the same narrow fix passed in both conditions, while BrownPants supplied a source-linked map of where test-path classification affects inventory, investigation, impact output, and candidate QA paths.

## Repository Evidence

- `src/BrownPants.Cli/Analysis/RepositoryScanner.cs:49` classifies discovered files into `TestPaths`; lines 128-134 implement exact `test`/`tests` directory and filename classification.
- `src/BrownPants.Cli/Analysis/RepositoryInvestigator.cs:21-27` marks clue matches as test-path candidates using `IsTestPath`.
- `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs:111-120` resolves caller paths and marks each call-path candidate using `IsTestPath`.
- The current `brownpants impact` run on `RepositoryScanner.cs` reports 49 direct and 138 transitive caller paths, 20 files, 2 projects, 12 test-path candidates, and maximum depth 8. These counts describe the entire changed file and are not exclusive to `IsTestPath`.
- The prior formal comparison records identical source/task inputs, the same fix, and focused/full passes in both worktrees. Two earlier setup attempts lacked completed turn records and are outside the formal pair.

## Design

- Lead with a plain-language scenario: a test-classification regression can mislabel evidence in multiple BrownPants reports, not just omit a fixture from an inventory list.
- Present a compact text flow from the classifier to inventory, investigation, and impact reports; attach exact source citations.
- Explain the operational payoff: developers see likely downstream surfaces and QA gets concrete candidate paths to inspect, while a human still validates coverage and runtime behavior.
- Keep the one-pair result and higher token usage visible but subordinate to the main value story. Do not claim causal quality or productivity improvement.
- Retain detailed setup and hash data in the report. Explain formal run status and excluded preliminary attempts in one concise note.

## Validation

Check every statement against current source, the recorded formal-run verification, and the observed BrownPants output. Run acceptance-hash/evidence validators and `git diff --check`. Do not run application tests for documentation-only changes.
