# Azure DevOps Integration Contract

## Configuration

The repository may contain `.brownpants/azure-devops.json` with `schemaVersion: 1`, organization URL, project, Azure Repos repository, authentication scheme, and feature-to-work-item mappings. `entra-browser` also requires a public `entraClientId` and explicit `entraTenantId`; these identify an app and tenant and are not secrets. Secret values and environment-variable names are never valid configuration properties. Browser auth is for Azure DevOps Services. Token auth reads `BROWNPANTS_AZURE_DEVOPS_TOKEN` only for an explicitly requested online operation.

## Commands

- `brownpants devops trace <repository> --feature <relative-feature-path> --online [--allow-custom-host] [--config <path>] [--format markdown|json]` performs bounded Azure DevOps reads and writes a deterministic traceability report to stdout. `--online` is the point where browser sign-in or environment-token lookup may begin.
- `brownpants devops pr preview <repository> --feature <relative-feature-path> --source <branch> --target <branch> --online [--allow-custom-host] [--output <relative-path>] [--overwrite]` creates a local review artifact from the fixed criteria baseline, task/evidence ledger, and available delivery links. `--online` is required only when Azure DevOps references should be resolved. Existing output is protected unless `--overwrite` is supplied.
- `brownpants devops pr create <repository> --preview <relative-path> --confirm --online [--allow-custom-host]` submits the exact reviewed title, description, branch targets, repository, and mapped work-item references as one Azure Repos draft PR. The destination repository must match configured scope. The request sets `isDraft: true` and omits reviewer and completion fields.

No command follows a link to a host supplied by an API response. Requests are built from the configured Azure DevOps base URL and documented API paths; response links are rendered only after host validation. Azure DevOps Services uses allowlisted service hosts. Custom Azure DevOps Server hosts require `--allow-custom-host` and HTTPS. Browser auth uses MSAL delegated Entra sign-in; a tenant administrator may need to approve the public-client app and its Azure DevOps permission. The signed-in user's Azure DevOps access level and ACLs still apply. BrownPants never echoes, serializes, or persists the acquired token. Server/headless token auth reads the fixed environment variable.

## Behavior

- Without `--online`, no HTTP request occurs.
- Read operations use versioned Azure DevOps REST endpoints. Each returned item includes its source URL and relationship evidence; unavailable relations are reported as unknown.
- Read failures return actionable diagnostics without claiming the item or relation is absent.
- Preview validation checks the acceptance-criteria digest, task states, ledger mappings, and evidence-file existence before describing work as verified.
- The PR preview carries a QA handoff: each acceptance criterion points to documented verification guidance and existing evidence, or is marked as a gap. It may include observed story/task relationships and returned build/release links, but it does not invent test steps or change work-item state.
- PR creation requires both a saved/reviewed preview and `--confirm`; one API POST creates a draft PR and associates mapped work items through the create request. The description uses neutral wording to avoid work-item state transitions. The command never calls reviewer, PR update, completion, merge, work-item update, build queue, or release create APIs.
- Markdown and JSON reports use stable ordering and repository-relative local paths.
