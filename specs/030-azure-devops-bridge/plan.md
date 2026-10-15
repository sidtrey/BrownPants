# Implementation Plan: Azure DevOps Sprint and Delivery Traceability

**Branch**: `030-azure-devops-bridge` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Add an opt-in Azure DevOps adapter that reads mapped work-item and delivery evidence, then builds a local PR preview tied to the existing acceptance digest and task/evidence ledger. A second, explicit command may submit that reviewed preview as a draft Azure Repos Git PR. Keep the existing CLI's offline inspection behavior independent of this integration.

## Technical Context

**Language/Version**: C# / .NET 10; Markdown and JSON configuration/reports  
**Primary Dependencies**: Existing CLI, `System.Net.Http`, and Microsoft Authentication Library for .NET (MSAL)
**Storage**: Repository-local mapping/configuration and optional local preview; no BrownPants-managed token persistence
**Testing**: xUnit with injected `HttpMessageHandler`, CLI tests, and cross-platform package smoke  
**Target Platform**: Windows, macOS, Linux; Azure DevOps Services first, with custom Server URLs only where API behavior is verified  
**Project Type**: .NET CLI provider integration  
**Performance Goals**: Bounded requests and response samples; report normal work-item sets without unbounded pagination  
**Constraints**: No requests by default; allowlisted Azure DevOps Services hosts; explicit HTTPS custom-host opt-in; browser-based Entra or fixed environment-only token auth; stable outputs; only one confirmed draft-PR POST; no reviewer, approval, merge, completion, work-item field/state, build, or release writes
**Scope**: Connection and feature mappings, traceability report, local PR preview, explicit Azure Repos draft-PR create operation, docs and tests

## Constitution Check

- **Evidence First**: Each linked artifact carries its source URL and observed relationship; missing relationships remain unknown.
- **Broad, Engine-Neutral Core**: Provider-specific configuration and commands remain isolated under an Azure DevOps integration surface; Spec Kit artifacts supply trace evidence without making the core scanner Spec Kit-only.
- **Deterministic and Reviewable Output**: Stable ordering, versioned JSON, local preview, and an explicit create step.
- **Safe Inspection by Default**: `inspect` and other offline commands do not load credentials or call the network. All online work is explicit and destination constrained.
- **Specified and Verified Changes**: Every implementation task maps to frozen AC/SC identifiers and mock-based evidence. PR submission tests enforce the exact write boundary.

Gate: PASS, provided the HTTP client exposes only the allowed read operations and one explicitly confirmed draft-PR create operation.

## Architecture and Data Flow

1. Parse and validate `.brownpants/azure-devops.json` without reading the credential.
2. After `--online`, use MSAL system-browser delegated sign-in for configured Azure DevOps Services auth or read `BROWNPANTS_AZURE_DEVOPS_TOKEN` for headless/Server auth. Reject unapproved cloud hosts and require explicit custom-host opt-in for Server; redact request/exception details containing secrets.
3. Map the selected feature path to configured work-item IDs. Issue versioned GET requests only to the configured Azure DevOps host and bounded endpoints.
4. Normalize work-item fields and returned relations into a deterministic report with diagnostics and completeness status.
5. Build a local PR preview by checking the acceptance digest, task list, evidence ledger, and local evidence paths; include missing proof as a visible gap.
6. On a separate `pr create` command, validate the saved preview and confirmed source/target/repository inputs, then send one POST creating `isDraft: true`. Return the provider's PR URL and ID.

The integration will not add a persistent token cache or perform interactive identity acquisition. Operators use their approved Entra token flow; a PAT environment-variable source is available only for legacy/server or short-lived local use. Do not follow API-provided links as request targets or log raw request headers and response bodies. Escape server-provided labels in Markdown.

## Project Structure

```text
src/BrownPants.Cli/Analysis/AzureDevOpsConfiguration.cs
src/BrownPants.Cli/Analysis/AzureDevOpsClient.cs
src/BrownPants.Cli/Analysis/AzureDevOpsTraceabilityAnalyzer.cs
src/BrownPants.Cli/Analysis/PullRequestPreviewAnalyzer.cs
src/BrownPants.Cli/Analysis/AzureDevOpsPullRequestSubmitter.cs
src/BrownPants.Cli/Output/AzureDevOpsReportRenderer.cs
src/BrownPants.Cli/Output/PullRequestPreviewRenderer.cs
tests/BrownPants.Tests/AzureDevOps*Tests.cs
integrations/azure-devops/README.md
tests/AzureDevOpsIntegrationTests.ps1
```

**Structure Decision**: Follow the existing analyzer/renderer pattern in `src/BrownPants.Cli`. Keep connection parsing, HTTP handling, report normalization, and PR submission separately testable. CLI dispatch stays in `Program.cs`.

## Complexity Tracking

No constitution exception is requested. The remote create path is intentionally a single narrow operation because the provider's PR creation scope can carry broader capabilities than this command uses.
