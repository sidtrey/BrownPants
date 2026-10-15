# Implementation Plan: GitHub Draft Pull Requests

**Branch**: `039-github-draft-pull-requests` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Reuse the evidence-rich saved PR preview for a provider-neutral offline workflow, then add an explicit GitHub.com adapter that submits only a confirmed draft PR. Preserve the existing Azure DevOps commands and their distinct authentication/configuration contract. Use the documented GitHub create endpoint, `draft: true`, and the narrow repository permission documented for that operation; do not add a runtime package.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing CLI and `System.Net.Http`; no new runtime package
**Storage**: Local feature artifacts and explicitly selected preview path; no credential persistence
**Testing**: xUnit, temporary repositories, injected `HttpMessageHandler`, packed-tool smoke
**Target Platform**: Windows, macOS, Linux; GitHub.com only
**Project Type**: .NET CLI provider adapter
**Performance Goals**: One bounded create request per confirmed operation
**Constraints**: No network by default; require both `--online` and `--confirm`; fixed environment token; reject redirects; no automatic retry after ambiguous create; only one draft-PR POST; no other provider mutation
**Scale/Scope**: One PR body bounded by the existing preview's file-size and CLI limits; no pagination or provider discovery

## Constitution Check

- **Evidence First**: Reuse criteria digest, task ledger, existing evidence, and explicit gaps. Provider success is reported only when the response confirms a draft and provides a URL/number.
- **Broad, Engine-Neutral Core**: Keep local preview creation provider-independent; isolate GitHub API behavior. Azure DevOps remains an adapter, not a prerequisite.
- **Deterministic and Reviewable Output**: Preview stays a stable local artifact, with immutable content digest verified before submission. Creation requires explicit provider, confirmation, and online opt-in.
- **Safe Inspection by Default**: Preview never requests credentials or accesses the network. GitHub create targets only `api.github.com`; no redirect following or response-provided request URLs.
- **Specified and Verified Changes**: Mock the complete HTTP contract, verify no-request paths, preserve Azure tests, and include packed CLI validation.

Gate: PASS. The only remote mutation is a user-confirmed GitHub draft PR create. GitHub requires repository-level Pull requests write permission for this endpoint; the token remains outside BrownPants storage.

## Research Decisions

See [research.md](research.md). GitHub's official REST API defines `POST /repos/{owner}/{repo}/pulls`, accepts `title`, `body`, `head`, `base`, and `draft`, and lists the fine-grained token permission as Pull requests: write. Pin the API version header to the current documented `2026-03-10`. V1 rejects Enterprise/custom API hosts.

## Architecture and Data Flow

1. `pr preview` reads only the selected repository's spec, tasks, baseline, and evidence ledger; it creates a signed-by-digest local review artifact without Azure configuration or network access.
2. `pr create` parses provider, explicit gates, and repository-contained preview path. It verifies preview integrity and repository/branch metadata before reading the fixed `BROWNPANTS_GITHUB_TOKEN` environment variable.
3. The GitHub adapter posts one JSON request to the fixed GitHub.com endpoint with the preview title/body/source/target and `draft: true`. HTTP redirects are disabled; errors are reduced to safe status/code messages, never raw headers or bodies.
4. A successful result requires HTTP Created and response fields confirming `draft: true`, a positive PR number, and a valid `html_url` on `github.com`. Transport ambiguity is reported without retry.
5. Existing `devops pr preview/create` remains available and keeps its Azure DevOps configuration and Entra/PAT behavior.

## Project Structure

```text
src/BrownPants.Cli/Analysis/PullRequestPreviewAnalyzer.cs
src/BrownPants.Cli/Output/PullRequestPreviewRenderer.cs
src/BrownPants.Cli/Analysis/GitHubPullRequestClient.cs
src/BrownPants.Cli/Analysis/GitHubPullRequestCommand.cs
src/BrownPants.Cli/Program.cs
tests/BrownPants.Tests/GitHubPullRequestTests.cs
tests/BrownPants.Tests/PullRequestPreviewCommandTests.cs
tests/PackedGitHubPullRequestSmoke.ps1
integrations/github/README.md
specs/039-github-draft-pull-requests/
```

**Structure Decision**: Keep provider-independent preview parsing/rendering in the existing analysis/output surfaces. Isolate GitHub HTTP/auth/result validation and CLI option handling under new analysis types. Add no project or dependency.

## Complexity Tracking

No constitution violations. The adapter intentionally supports GitHub.com only; custom host and other provider behavior require separate evidence and tests.
