# GitLab.com Draft Merge Requests

BrownPants can submit an integrity-checked local PR preview as one GitLab.com draft merge request. Self-managed GitLab is not supported.

```powershell
$env:BROWNPANTS_GITLAB_TOKEN = '<approved-token>'
brownpants pr create . --provider gitlab --preview artifacts/pr-preview.md --repository group/project --source feature/example --target main --confirm --online
```

Use an approved personal, project, or group access token with API access and permission to create merge requests in the target project. GitLab checks token scopes and project permissions; broad API scopes grant substantial access, so use your organization's approved credential policy. Provide the token through your approved credential store or process environment; BrownPants does not save or print it. Both `--confirm` and `--online` are required. The command validates preview integrity and repository/branch matches before reading the token, then sends one request to the fixed GitLab.com API. It prefixes the title with `Draft:` and verifies GitLab reports an open draft with a GitLab.com URL. See the [GitLab Merge Requests API](https://docs.gitlab.com/api/merge_requests/) and [REST authentication](https://docs.gitlab.com/api/rest/authentication/).

Review the created merge request in GitLab. BrownPants does not request reviewers, approve, merge, close, or edit the merge request, and it does not retry uncertain outcomes. If the request times out or the response is incomplete, inspect GitLab before trying again.
