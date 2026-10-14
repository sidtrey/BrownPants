# Implementation Plan: Acceptance Criteria Integrity

**Branch**: `021-acceptance-criteria-integrity` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Close two gaps in the existing acceptance hash gate. A read-only validator will require a complete task-to-criterion evidence ledger when a task file changes. A pull request policy will compare existing baselines with the base commit and require a reviewer label plus a matching review record for amendments.

## Technical Context

**Language/Version**: PowerShell 7 (`pwsh`) for repository automation; C#/.NET 10 remains the product CLI.
**Primary Dependencies**: PowerShell, Git, GitHub Actions, existing SHA-256 baseline script; no new runtime package.
**Storage**: Per-feature `acceptance-evidence.json` and, for approved changes, `acceptance-criteria-review.md`.
**Testing**: PowerShell fixture tests with temporary Git repositories; existing xUnit, Spec Kit, OpenSpec, and packed-tool checks.
**Target Platform**: Windows, Ubuntu, and macOS CI runners.
**Project Type**: Repository workflow automation and Spec Kit adapter guidance.
**Performance Goals**: Validate only changed task artifacts and baseline paths; finish within the existing CI job.
**Constraints**: Read-only validation, no commands from evidence ledgers are executed, stable diagnostics, approval tied to the pull request base/head, and no edits to frozen acceptance sections without explicit review.
**Scale/Scope**: Spec Kit features with acceptance baselines, changed task artifacts, and deliberate baseline amendments.

## Constitution Check

- **Evidence First**: PASS. The ledger points to observed tests and commands while stating that structural checks cannot prove semantic coverage.
- **Broad, Engine-Neutral Core**: PASS. The ledger and criteria IDs describe SDD work generally; Spec Kit and GitHub Actions provide workflow adapters.
- **Deterministic and Reviewable Output**: PASS. Validation has stable ordering and actionable failures; review records preserve the old and new digests.
- **Safe Inspection by Default**: PASS. Scripts read Markdown, JSON, Git history, and path metadata; they do not execute ledger commands or modify repository content.
- **Specified and Verified Changes**: PASS. The new spec is frozen before implementation and PowerShell fixtures cover valid and invalid histories.

## Architecture

1. `scripts/check-acceptance-evidence.ps1` reads the feature spec, task list, and JSON ledger. It validates stable criterion IDs, task IDs, complete coverage, task status, verification commands, and repository-relative evidence paths. It never invokes those commands.
2. `scripts/check-acceptance-criteria-review.ps1` compares baseline paths in a base/head Git diff. A new sidecar is an initial baseline. An existing sidecar cannot be deleted; modification requires the `acceptance-criteria-reviewed` label and a changed, feature-local review record whose old/new digests match Git history.
3. CI runs evidence validation for changed `tasks.md` files and runs baseline review validation on pull request events. It reruns for label changes and fetches sufficient history to compare the PR base and head.
4. Spec Kit context guidance requires `AC-###` and `SC-###` mapping, actual evidence records, baseline verification before and after implementation, and maintainer review before rebaselining.

## Project Structure

```text
scripts/check-acceptance-evidence.ps1
scripts/check-acceptance-criteria-review.ps1
tests/AcceptanceEvidenceTests.ps1
tests/AcceptanceCriteriaReviewTests.ps1
specs/021-acceptance-criteria-integrity/
  contracts/acceptance-evidence.schema.json
  contracts/acceptance-criteria-review.md
  acceptance-evidence.json
```

The checkers remain PowerShell scripts because the repository already uses PowerShell for cross-platform Spec Kit and hash automation. No new CLI command or C# dependency is justified for these workflow checks.

## Decision and Risks

- A reviewer applies `acceptance-criteria-reviewed` only after checking the spec diff and review record. The label is a workflow signal, not cryptographic proof of reviewer judgment.
- Structural validation confirms every criterion/task has a trace and every completed task has recorded evidence; it does not establish that the referenced test fully proves the behavior.
- Historical task files are not rewritten. Evidence validation runs when a task file changes, allowing features to adopt the ledger incrementally.
- Pull request checks must be required by repository rules for merge blocking. Direct pushes can bypass pull request review policy; CI still reports current baseline mismatches.

## Acceptance Criteria Integrity

The scenarios and success criteria are frozen in [acceptance-criteria.sha256](acceptance-criteria.sha256). Verify with `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/021-acceptance-criteria-integrity/spec.md`. Do not refresh without explicit user review.
