# Acceptance Criteria Review: Azure DevOps Draft PR Linking

**Review status**: Approved by the maintainer in conversation on 2026-10-14
**Reason**: The original AC-008 prohibited all work-item mutation, which contradicted the requested association of mapped work items when creating an Azure Repos draft PR. The change limits linked behavior to the PR create request and forbids changing work-item fields or workflow state.

## Digests

- **Previous**: `7405098f91b941d3f3e60d75caa6e89546a8aa63180de33529b41239994c3aa8`
- **Proposed**: `129d67b776ce21308172352baf9e8ed22f85e21a79a3343864630ce4ac843f86`

## Changed criterion

- **AC-008 (previous)**: Never mutates work items.
- **AC-008 (approved)**: Never changes work-item fields or workflow state; a draft PR create request may associate mapped work items. All other PR approval, vote, auto-complete, merge, completion, update, and abandon operations remain prohibited.

The approved wording is recorded in `spec.md`; `acceptance-criteria.sha256` is refreshed to the proposed digest after this review.

## QA handoff criteria amendment

**Review status**: Approved by the maintainer in conversation on 2026-10-14
**Reason**: The team's sprint workflow needs a usable QA handoff that connects frozen acceptance criteria to documented test guidance, available evidence, and delivery links. The handoff must expose missing guidance and avoid inventing test steps.

### Digests

- **Previous**: `129d67b776ce21308172352baf9e8ed22f85e21a79a3343864630ce4ac843f86`
- **Proposed**: `232c6c7071d40e7b6677a8772c744223ec83ba4450b95efd7c614541a7f8ea51`

### Changed criteria

- **AC-009**: Add a per-acceptance-criterion QA handoff in PR preview using documented verification guidance, evidence, and known delivery links; show unsupported test guidance as a gap.
- **SC-007**: Verify that QA can find each criterion, guidance or gap, evidence, and known build/release links from the preview.

The approved criteria are recorded in `spec.md`; the baseline is refreshed to the proposed digest after this review.

## Interactive browser authentication amendment

**Review status**: Approved by the maintainer in conversation on 2026-10-14
**Reason**: Teams with limited PAT or CLI access need a first-party-feeling browser sign-in path that respects each user's Azure DevOps permissions and organization policies.

### Digests

- **Previous**: `232c6c7071d40e7b6677a8772c744223ec83ba4450b95efd7c614541a7f8ea51`
- **Proposed**: `a7edacc2a80cdd97f8ba811147f9335a7b821375ede4119f0e228a52ff5e1d84`

### Changed criteria

- **AC-010**: Add delegated Microsoft Entra system-browser sign-in for Azure DevOps Services; preserve explicit token auth for headless/server use and fail closed if sign-in is denied or cancelled.
- **SC-008**: Verify online gating, cancellation/no requests, and no token persistence or output using a mockable authentication provider.

The approved criteria are recorded in `spec.md`; the baseline is refreshed to the proposed digest after this review.
