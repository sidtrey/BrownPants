# Implementation Plan: Release Tag Acceptance Ledger Scope

**Branch**: `045-tag-ci-ledger-scope` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Extract changed Spec Kit artifact selection into a testable PowerShell script. Preserve pull request and ordinary push comparisons; for a new tag with all-zero `before`, compare the tag commit to its parent. Use full-tree discovery only when a branch starts from a root commit or a root tag has no parent. Run the workflow contract test in CI and publish a patch release after branch and tag CI pass.

## Technical Context

**Language/Version**: PowerShell 7 and GitHub Actions
**Dependencies**: Existing Git executable and PowerShell; no packages
**Storage**: Git object history only
**Testing**: Temporary Git repositories in `tests/AcceptanceCriteriaIntegrityWorkflowTests.ps1`
**Target Platform**: GitHub Actions Windows, Ubuntu, macOS
**Constraints**: No weakening for changed features; no late first acceptance baselines or fake completion records; tag builds still run build/test/package checks.
**Scale/Scope**: One selection helper, workflow integration, cross-platform tests, patch-release metadata.

## Constitution Check

- Evidence First: the changed-file list is derived from Git and tested against real temporary histories.
- Deterministic/reviewable: stable path output and explicit event cases.
- Safe by default: no network or repository mutation beyond temporary test commits.
- Specified/verified: acceptance criteria hash is pinned and tag behavior is covered by scenarios.

Gate: PASS. The fix limits ledger validation to the tag's changed files while retaining strict checks for every changed feature.

## Project Structure

- `scripts/get-changed-spec-artifacts.ps1`: Git event range selection
- `.github/workflows/ci.yml`: pass event/ref metadata and validate selected changed paths
- `tests/AcceptanceCriteriaIntegrityWorkflowTests.ps1`: workflow assertions plus temporary repository cases
- `specs/045-tag-ci-ledger-scope/`: frozen criteria, plan, tasks, ledger, and verification
- `src/BrownPants.Cli/BrownPants.Cli.csproj`, install instructions, extension manifest, and release notes: patch version

## Verification

Run the workflow contract test on Windows, full Release build/tests, acceptance hash and evidence checks, vulnerability audit, packed tool smoke, network-isolated package smoke, branch CI, and new tag CI. The acceptance-ledger test must prove an unrelated legacy folder is excluded for tag pushes while the tag-changed ledger remains selected.
