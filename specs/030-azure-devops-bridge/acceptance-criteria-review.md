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
