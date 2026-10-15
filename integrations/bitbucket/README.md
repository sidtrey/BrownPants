# Bitbucket Cloud Draft Pull Requests

BrownPants can submit an integrity-checked local PR preview as one Bitbucket Cloud draft pull request. Bitbucket Data Center is not supported.

```powershell
$env:BROWNPANTS_BITBUCKET_TOKEN = '<approved-token>'
brownpants pr create . --provider bitbucket --preview artifacts/pr-preview.md --repository workspace/repository-slug --source feature/example --target main --confirm --online
```

Use an approved Bitbucket Cloud repository, project, or workspace access token with `pullrequest:write` scope for the target repository. That permission also allows merging or declining PRs at the provider; BrownPants itself sends only a create request. Supply the token through your approved credential store or process environment; BrownPants does not save or print it. Both `--confirm` and `--online` are required. The command validates preview integrity and repository/branch matches before reading the token, then sends one request to the fixed Bitbucket Cloud API with `draft: true`. See the [create pull request API](https://developer.atlassian.com/cloud/bitbucket/rest/api-group-pullrequests/) and [scope reference](https://developer.atlassian.com/cloud/bitbucket/bitbucket-cloud-rest-api-scopes/).

Review the created pull request in Bitbucket. BrownPants does not request reviewers, approve, merge, decline, or edit the pull request, and it does not retry uncertain outcomes. If the request times out or the response is incomplete, inspect Bitbucket before trying again.
