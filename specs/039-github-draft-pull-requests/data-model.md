# Data Model: GitHub Draft Pull Requests

## Saved PR preview

Reuse schema version 1 from `PullRequestPreviewRenderer`.

- `featurePath`: repository-relative feature directory.
- `title`, `description`: reviewed pull request title and description.
- `repository`: explicit `owner/name` target for the GitHub preview.
- `sourceBranch`, `targetBranch`: branch names carried in preview metadata.
- `workItemIds`: existing Azure DevOps evidence only; never reinterpret as GitHub issue IDs.
- `contentDigest`: SHA-256 over normalized preview metadata and the complete rendered body.

Before any provider token is read, verify the digest with fixed-time comparison and require the provider, repository, and branch metadata to match the explicit create selection.

## GitHub create request

- `title`: saved preview title.
- `body`: exact reviewed preview body.
- `head`: saved source branch.
- `base`: saved target branch.
- `draft`: always `true`.

The adapter MUST NOT send reviewer, team, assignee, labels, milestone, maintainer, merge, or issue-mutation fields.

## Create result

- `number`: positive provider-assigned PR number.
- `url`: absolute HTTPS URL whose host is exactly `github.com`.
- `isDraft`: true, required before reporting success.

## Credential

`BROWNPANTS_GITHUB_TOKEN` is process-environment input only. It is never serialized, persisted, included in errors, or loaded by preview or help commands.
