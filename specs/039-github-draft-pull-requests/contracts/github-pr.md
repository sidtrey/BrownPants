# GitHub Draft PR CLI and HTTP Contract

## Commands

Create a provider-neutral local preview without network access or Azure configuration:

```text
brownpants pr preview <repository-root> --feature <relative-feature-path> --repository <owner/name> --source <branch> --target <branch> [--output <relative-path>] [--overwrite]
```

Submit a previously reviewed preview to GitHub.com:

```text
brownpants pr create <repository-root> --provider github --preview <relative-path> --repository <owner/name> --source <branch> --target <branch> --confirm --online
```

The preview path must resolve inside the selected repository. Create-time repository and branch values must exactly match the reviewed preview. Existing preview output is protected unless `--overwrite` is supplied. Both flags are required before environment credential lookup or network activity.

Preview returns exit code `0` when its required criteria/evidence are complete and `1` when gaps remain. In either case it renders the local preview so the user can review the reported gaps.

## Request

- Method/path: `POST https://api.github.com/repos/{owner}/{repo}/pulls`
- Headers: `Accept: application/vnd.github+json`, `Authorization: Bearer <BROWNPANTS_GITHUB_TOKEN>`, `X-GitHub-Api-Version: 2026-03-10`, GitHub-required user agent.
- JSON body contains only `title`, `body`, `head`, `base`, and `draft: true` from the validated preview.
- Redirects are disabled. No other endpoint is permitted.

## Success and failure

Success requires HTTP 201 and valid response JSON with `number > 0`, `draft: true`, and an HTTPS `html_url` on `github.com`. On status/API errors, invalid responses, or transport errors, return a concise diagnostic with no token or raw response body. Do not retry. The user must inspect GitHub after an ambiguous failure.

Existing `devops pr preview` and `devops pr create` continue to follow the Azure DevOps integration contract.
