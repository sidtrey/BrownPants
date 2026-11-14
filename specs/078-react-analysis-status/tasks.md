# Tasks: Explicit React Candidate Analysis Status

## Phase 1: Report status

- [X] T001 [US1] Add a provider-agnostic candidate-analysis status to `ReactSupportAnalysis`, defaulting to `not-requested` and set from parser verification results.
- [X] T002 [US1] Update Markdown so it distinguishes unrequested, verified/partial, unavailable, and failed candidate analysis without presenting unavailable counts as zero findings.
- [X] T003 [US1] Add focused tests covering default, verified, unavailable, failed, JSON, and Markdown status behavior.

## Phase 2: Verify against FullMetalFile

- [X] T004 [US1] Run BrownPants build and focused tests; inspect FullMetalFile once with default settings and once with `--typescript-syntax`.
- [X] T005 [US1] Update FullMetalFile evidence, run BrownPants changed-path inspection, and verify the acceptance digest and evidence ledger.
