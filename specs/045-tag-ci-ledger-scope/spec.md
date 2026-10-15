# Feature Specification: Release Tag Acceptance Ledger Scope

**Feature Branch**: `045-tag-ci-ledger-scope`
**Created**: 2026-10-15
**Status**: In Progress
**Input**: Release validation revealed that a new annotated tag has an all-zero push-before SHA; CI scans all historical task artifacts and fails on unchanged legacy features without ledgers.

## User Scenarios & Testing

### User Story 1 - Validate a release tag without auditing unrelated history (Priority: P1)
A maintainer tags a commit that already passed branch CI. The tag workflow validates the files changed by that tagged commit, so unchanged historical feature directories do not block release verification.

**Independent Test**: Run the acceptance-ledger workflow check against simulated branch, tag, and root-commit push metadata.

**Acceptance Scenarios**:
1. **AC-001** Given a tag push with an all-zero `before` SHA and an existing parent commit, when CI determines changed feature artifacts, then it compares the tag commit with its parent and validates only changed task/evidence ledgers.
2. **AC-002** Given an ordinary non-initial branch push, when CI determines changed artifacts, then it continues to compare the event's `before` and `head` commits.
3. **AC-003** Given a first/root commit with no parent and an all-zero `before` SHA, when CI determines changed artifacts, then it validates the complete tree.
4. **AC-004** Given the tagged release commit itself changes a task list or acceptance evidence ledger, when tag CI runs, then the changed feature ledger is still validated and can fail on invalid evidence.

## Requirements

- **FR-001**: CI MUST distinguish tag pushes from branch pushes when `github.event.before` is all zero.
- **FR-002**: A tag with a parent MUST diff that tag commit against its parent for changed Spec Kit task/evidence files.
- **FR-003**: A branch's normal before/head comparison MUST remain unchanged; an initial root commit MUST fall back to whole-tree discovery.
- **FR-004**: Changed feature artifacts MUST continue to require valid specifications and acceptance evidence ledgers; completed tasks MUST NOT be exempted.
- **FR-005**: Automated workflow tests MUST cover the three push cases and preserve the closed-feature disposition behavior.

## Success Criteria

- **SC-001**: CI on a newly pushed release tag passes when the tag's feature ledgers pass, regardless of unchanged legacy feature folders without ledgers.
- **SC-002**: A malformed ledger in a feature changed by the tag still causes validation failure.
- **SC-003**: Branch push and root-commit fallback behavior remain covered by tests.
- **SC-004**: The targeted workflow test, full test suite, and cross-platform tag CI pass.

## Assumptions

- Branch CI validates the feature changes before a maintainer tags a release.
- New tags point to commits with a parent except for a repository's initial root tag.
- Historic feature tasks created before acceptance-ledger enforcement remain outside scope unless changed.
