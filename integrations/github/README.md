# GitHub Draft Pull Requests

BrownPants prepares a local review preview from a feature's frozen acceptance criteria, task evidence, QA guidance, and documented gaps. Preview creation is offline and works without Azure DevOps configuration:

```powershell
brownpants pr preview . --feature specs/039-github-draft-pull-requests --repository sidtrey/BrownPants --source feature/039-github-draft-pull-requests --target main --output artifacts/039-pr-preview.md
```

Inspect the generated file before submitting it. It includes an integrity digest; edits after review cause creation to stop. Incomplete evidence remains visible and is never described as verified.
The preview command returns `0` when the reviewed feature evidence is complete and `1` when it finds gaps; either result still produces the requested preview so the user can inspect it.

## Create a draft

Set `BROWNPANTS_GITHUB_TOKEN` through your approved credential manager or process environment. The token needs repository **Pull requests: write** permission. Do not put it in repository configuration, command-line arguments, or logs.

```powershell
brownpants pr create . --provider github --preview artifacts/039-pr-preview.md --repository sidtrey/BrownPants --source feature/039-github-draft-pull-requests --target main --confirm --online
```

Both `--confirm` and `--online` are mandatory. BrownPants reads the token only after validating the preview and matching the explicit repository and branches. It sends one request to GitHub.com to create a draft pull request and reports the returned number and URL. If a timeout or ambiguous response occurs, inspect GitHub before retrying; BrownPants never retries automatically.

## Boundaries

- V1 supports GitHub.com only. GitHub Enterprise Server and other Git providers are not supported by this adapter.
- BrownPants never requests reviewers, approves, merges, closes, or edits the PR. It does not modify issues or queue builds, checks, or releases.
- Review, approval, merge, and completion remain human-controlled. GitHub repository policy and the token's permission still apply.
- Draft PR availability varies by repository and account plan. If GitHub refuses draft creation, BrownPants reports the failure; it never falls back to a ready-for-review PR.
- Azure DevOps traceability and `devops pr create` remain separate. Azure work item IDs are not treated as GitHub issues.

The implementation follows [GitHub's pull request creation API](https://docs.github.com/en/rest/pulls/pulls#create-a-pull-request). Fine-grained tokens require repository Pull requests write permission for that endpoint.
