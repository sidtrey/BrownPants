# Research: GitHub Draft Pull Requests

## Decisions

- **GitHub.com only for the first additional provider.** The repository already uses GitHub, and the provider exposes a single REST endpoint that supports creating a draft PR with the exact title, body, head, and base from a reviewed preview. Defer GitHub Enterprise Server and other providers until their host/auth contracts have separate verification.
- **Use the existing saved PR preview.** It already carries a SHA-256 digest over metadata and body, plus task/criteria QA evidence. The new provider-independent preview command must work with no Azure DevOps configuration; the existing Azure commands remain unchanged.
- **Use one explicitly gated request.** Require `--provider github --confirm --online`. Read only `BROWNPANTS_GITHUB_TOKEN` after validating the preview and all options. Do not retry ambiguous failures or follow redirects.
- **Use no added package.** `System.Net.Http` and `System.Text.Json` are sufficient and keep the CLI's dependency surface trusted and small.
- **Do not infer GitHub issue links from Azure work item IDs.** Include the complete reviewed preview body. Preserve any existing evidence URLs as content, but make no provider-specific work item changes.

## Evidence

The GitHub REST API's create-pull-request endpoint is `POST /repos/{owner}/{repo}/pulls`. Its documented request accepts title, body, head, base, and a `draft` boolean. Fine-grained tokens require repository `Pull requests` write permission; a successful response is HTTP 201. The API documentation currently shows version header `2026-03-10`.

Source: [GitHub REST API: Create a pull request](https://docs.github.com/en/rest/pulls/pulls#create-a-pull-request)

The local Azure DevOps feature already supplies a reusable integrity-checked preview, but `devops pr preview` currently loads Azure DevOps configuration unconditionally. The new generic preview path should invoke `PullRequestPreviewAnalyzer` and `PullRequestPreviewRenderer` directly, with no provider client or configuration.

## Risks and boundaries

- GitHub's draft feature availability varies by plan and repository policy; the create command reports provider rejection without changing the request to non-draft.
- Network timeout can occur after the PR was created. Automatic retry could duplicate it, so the user must inspect GitHub before retrying.
- The fine-grained token's permission can exceed BrownPants' one-request code path. Documentation must state the provider-level permission boundary while tests enforce BrownPants' narrower behavior.
