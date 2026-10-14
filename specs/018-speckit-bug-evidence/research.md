# Research: Spec Kit Bug Assessment Evidence

## Decision: Use the official bug assessment pre-hook

The bundled Spec Kit bug assessment resolves `BUG_SLUG` and creates `BUG_DIR` before checking `before_bug_assess`, then ingests the report and writes `assessment.md`. Register one optional BrownPants hook at that seam.

**Rationale**: It provides the correct per-bug directory while keeping Spec Kit's existing assess/fix/test commands authoritative.

**Alternatives considered**: A competing BrownPants bug flow duplicates Spec Kit; a manual-only scan is undiscoverable; a mandatory global hook would burden repositories not using the bundled bug workflow.

**Evidence**: [Spec Kit bug assessment command](https://github.com/github/spec-kit/blob/main/extensions/bug/commands/speckit.bug.assess.md), [bug workflow guide](https://github.com/github/spec-kit/blob/main/docs/guides/bugfix.md), [hook implementation PR #4878](https://github.com/github/spec-kit/pull/4878), closing [issue #4799](https://github.com/github/spec-kit/issues/4799).

## Decision: Reuse explicit literal investigation clues

Select bounded, distinctive clues from pasted report text already in the conversation and pass them to the existing `brownpants investigate` command.

**Rationale**: The CLI already provides deterministic matching, limits, and no-snippet reports. URL fetching or semantic extraction would add risk and duplicate Spec Kit policy.

## Decision: Optional, fail-open behavior

If safe clues, the CLI, or a writable bug directory are unavailable, report that evidence was skipped and let Spec Kit continue.

**Validation note**: The live test must install the official bug extension before BrownPants and assert the generated optional registration.
