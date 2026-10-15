# Verification: Brownfield Value Demonstration

## Frozen acceptance criteria

- SHA-256: `a1b3ca59a7929e52d2a9a8fb955a9a22d058b95bccf2ecf3b4bc6fc314c80b4f`.
- The baseline was initialized before editing the README or comparison report.

## Source evidence checked

- `RepositoryScanner.cs:49` uses `IsTestPath` to populate `TestPaths`; `:128-134` contains the classifier.
- `RepositoryInvestigator.cs:23` labels each matching repository path with `IsTestPath`.
- `CallImpactAnalyzer.cs:118` labels resolved caller paths with `IsTestPath`.
- `RepositoryScannerTests.cs:151-177` checks investigation test-candidate labels and `:322-344` checks inventory classification.
- `CallImpactAnalyzerTests.cs:12-28` checks direct/transitive impact output and test-candidate labeling.
- BrownPants `impact` output for the scanner file reported 49 direct caller paths, 138 transitive paths, 20 files, 2 projects, 12 test-path candidates, and depth 8. These are whole-file results, not counts for `IsTestPath` alone.

## Formal experiment evidence

- Both formal Codex runs completed with the same source, seeded defect, task artifacts, prompt, model, and host.
- Both agents restored the same directory recognition rule. The experiment driver, rather than the agents, ran final tests.
- Both worktrees passed the focused suite (75 passed, 0 failed) and the complete Release suite (235 passed, 0 failed).
- The pre-run failure was expected because the experiment intentionally removed the directory check.
- Two preliminary baseline attempts had no completed turn records and were excluded from the measured pair and usage totals. The report now labels these separately from the completed formal pair.
- Detailed hashes, commands, usage source, and limitations are in `docs/comparisons/brownpants-vs-spec-kit-2026-10.md` and `specs/046-brownfield-sdd-comparison/verification.md`.

## Final documentation review

- README explains what Spec Kit supplies, what BrownPants added to this task, how a developer can use the impact map, and what QA can inspect.
- All published source links resolve to existing files and cited lines.
- Claims distinguish source-confirmed relationships from static candidates; no runtime, coverage, token-savings, or coding-quality superiority is claimed.
- No raw agent logs or credentials are included.
- This change edits Markdown and Spec Kit documentation only; application tests were not run.
