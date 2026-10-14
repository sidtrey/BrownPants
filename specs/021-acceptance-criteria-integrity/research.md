# Research: Acceptance Criteria Integrity

## Existing BrownPants Gate

`scripts/acceptance-criteria-hash.ps1` hashes the complete `User Scenarios & Testing` and `Success Criteria` sections. It detects criteria drift when the sidecar is unchanged, and CI verifies every committed sidecar. It does not compare a changed sidecar with the pull request base, so changing both the spec and digest in one change can pass. Task checkboxes also have no machine-checkable link to criteria or observed evidence.

## Design Decisions

### Require a task evidence ledger

Use a feature-local JSON ledger with one record per task. Every acceptance (`AC-###`) and success (`SC-###`) criterion must appear in at least one task record, and every task must map to at least one criterion. A completed task requires a `passed` result and an existing repository-relative evidence path. An incomplete task uses `planned`. This makes task coverage and recorded results machine-checkable without claiming the checker proves test quality.

### Compare existing baselines against the PR base

The review checker inspects the Git diff between base and head. A newly added sidecar is an initial commitment. A modified existing sidecar requires a matching review record and `acceptance-criteria-reviewed` label. Removing or renaming a baseline that exists on the base fails. The normal digest verifier still catches acceptance text changes whose sidecar was not updated.

The review record contains the previous digest, proposed digest, reason, and changed `AC-###`/`SC-###` identifiers. The reviewer label is deliberately applied by a maintainer after inspecting both the record and the spec diff; the script cannot infer human intent from prose.

### Rerun when review labels change

GitHub Actions runs pull-request workflows for `labeled` and `unlabeled` events, so adding or removing the review label re-evaluates the check. The pull request event payload includes the current labels. The checkout action fetches one commit by default; the workflow must set `fetch-depth: 0` so both comparison commits are available.

Do not use `pull_request_target`: the regular `pull_request` event is sufficient, and the workflow already builds and tests pull request content. The new checker only reads Git objects and files and does not run evidence commands.

## Sources

- [GitHub Actions events that trigger workflows](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows) documents `labeled` and `unlabeled` pull request activity types.
- [GitHub webhook events and payloads](https://docs.github.com/en/webhooks/webhook-events-and-payloads) documents the pull request event payload, including labels.
- [actions/checkout](https://github.com/actions/checkout) documents `fetch-depth: 0` for fetching all history.
