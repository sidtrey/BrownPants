# Acceptance Criteria Review Contract

## Pull Request Inputs

- Base and head Git revisions for the pull request.
- Current pull request labels, including `acceptance-criteria-reviewed` when a maintainer has reviewed the amendment.
- Changed spec, sidecar, and review-record paths.

## Policy

1. A baseline absent from the base revision does not require a review only when its feature spec is also absent from the base revision. Adding a late first baseline to an existing feature requires review; record `none` as the previous digest.
2. A baseline present on the base revision must not be deleted or renamed.
3. A modified existing baseline requires the review label and a review record changed in the same pull request.
4. The review record's previous digest must equal the base sidecar, and its proposed digest must equal the head sidecar.
5. The record must include a reason and at least one changed `AC-###` or `SC-###` ID.
6. If acceptance text changed but the sidecar did not, the normal acceptance hash verifier fails.
7. The check reads repository content and Git objects only; it does not write files, call the network, or execute evidence commands.

## Review Record Format

The feature-local `acceptance-criteria-review.md` uses the exact headings `Previous digest`, `Proposed digest`, `Reason`, and `Changed criteria`. Existing baselines use their 64-character SHA-256 value in `Previous digest`; a late first baseline uses `none`. The proposed digest contains one 64-character SHA-256 value. The changed criteria section contains one or more acceptance or success criterion IDs.

The check is a workflow signal. A maintainer must review the actual spec diff and configure the check as required for merge protection.
