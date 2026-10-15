# Implementation Plan: Multi-Provider Draft Pull Requests

**Branch**: `044-multi-provider-draft-prs` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Extend the saved evidence preview submission workflow with GitLab.com and Bitbucket Cloud adapters. Each adapter validates the saved preview and explicit metadata before loading credentials, pins requests to a fixed official host, disables redirects, creates one draft, validates the response, and never retries ambiguous outcomes. Reuse BCL HTTP/JSON APIs and the current command/handler test pattern.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing System.Net.Http and System.Text.Json; no new package
**Storage**: Existing repository-relative preview file
**Testing**: Existing xUnit suite, injected HttpMessageHandler
**Target Platform**: .NET 10 CLI on Windows, macOS, Linux
**Project Type**: CLI application
**Constraints**: GitLab.com REST API v4 and Bitbucket Cloud REST API 2.0 only; explicit online and confirmation gates; one mutation only; do not read credentials before validation; stable error messages; preserve GitHub and Azure Repos.
**Scale/Scope**: Two command adapters, tests, CLI help, provider docs, queue update.

## Constitution Check

- Evidence First: provider response and preview integrity are checked; no claim of merge safety.
- Broad, engine-neutral core: share the existing preview and leave provider code in adapters.
- Deterministic/reviewable: local preview stays offline and immutable at submission.
- Safe by default: no request absent both gates; fixed HTTPS API hosts; no redirects/retries.
- Specified and verified: acceptance digest is pinned; tests assert request count and payload.

Gate: PASS. Phase 1 review retains fixed provider hosts and no additional dependencies.

## Design

Add provider-specific command/client pairs modeled on `GitHubPullRequestCommand` and `GitHubPullRequestClient`; expose them through `Program.cs` `pr create --provider`. Keep parsing, preview integrity, path validation, and provider metadata validation ahead of credential reads. Use a single POST to GitLab `/api/v4/projects/{encoded-project}/merge_requests` with title prefixed `Draft:`; verify open state and a GitLab HTTPS web URL. Use a single POST to Bitbucket `/2.0/repositories/{workspace}/{repo_slug}/pullrequests` with `draft: true`; verify open state, draft response, and an `html.href` under `bitbucket.org`. Disable redirects and don't retry; redact credentials from all failures. Add docs under `integrations/gitlab` and `integrations/bitbucket`, CLI guidance, and tests for success, denial gates, tampering, invalid response, and ambiguous transport failure.

### Project Structure

- `src/BrownPants.Cli/Analysis/`: provider commands and API clients
- `src/BrownPants.Cli/Program.cs`: provider dispatch/help
- `tests/BrownPants.Tests/`: request shape and safety tests
- `integrations/{gitlab,bitbucket}/README.md`: setup and limits
- `README.md`, `IDEA-QUEUE.md`: user-facing availability and queue disposition

## Verification

Run acceptance hash validation and evidence-ledger validation, `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, focused provider tests, package audit, CLI help, and offline preview smoke. Do not use live credentials or contact providers in automated verification.
