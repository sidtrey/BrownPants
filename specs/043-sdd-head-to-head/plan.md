# Implementation Plan: Vanilla SDD and BrownPants Comparison

**Branch**: `043-sdd-head-to-head` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Run one paired controlled-bug task in detached worktrees from the same BrownPants commit. Use the same Codex CLI model and acceptance criteria. The baseline receives no BrownPants reports; the assisted condition receives the standard local BrownPants inventory and frozen-criteria evidence. Extract usage events from temporary JSONL and publish a small sanitized result in `docs/comparisons/` plus the README.

## Technical Context

**Language/Version**: Repository's .NET 10 solution
**Primary Dependencies**: Existing Codex CLI and BrownPants tool; no new packages
**Storage**: Temporary worktrees and temporary JSONL, then sanitized Markdown/JSON summary
**Testing**: Each run adds a regression test and executes focused plus full Release tests
**Target Platform**: Windows host; worktree paths local to this checkout
**Project Type**: Reproducible engineering comparison and product documentation
**Constraints**: Same commit/model/task, no raw trace commits, no claims beyond one pair, no reruns to select a preferred result
**Scale/Scope**: Two isolated runs, one controlled bug, same Codex CLI version/model

## Constitution Check

- Evidence First: preserve baseline reproduction and actual test output.
- Acceptance Integrity: both conditions use the same hash-pinned criteria.
- User Protection: no raw trace or credentials in repository; temporary worktrees removed after review.
- Honest Claims: report measured counts and outcomes; do not generalize from one pair.

Gate: PASS if provider-reported usage exists for both runs and the same task/commit/model controls are observed. If usage is missing, mark the token comparison unavailable and document the reason.

## Project Structure

```text
specs/043-sdd-head-to-head/
docs/comparisons/brownpants-vs-vanilla-sdd-2026-10.md
README.md
```
