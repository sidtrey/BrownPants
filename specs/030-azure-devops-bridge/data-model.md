# Data Model: Azure DevOps Sprint and Delivery Traceability

## Connection configuration

Repository-local `.brownpants/azure-devops.json`:

- `schemaVersion`: supported configuration version.
- `organizationUrl`: HTTPS Azure DevOps organization or configured Server base URL.
- `project`: project name or ID.
- `repository`: Azure Repos Git repository name or ID for PR creation.
- Credential: read only from the fixed `BROWNPANTS_AZURE_DEVOPS_TOKEN` environment variable after explicit online opt-in; never from repository config.
- `authenticationScheme`: `bearer` or `pat`.
- `features`: mapping from a repository-relative Spec Kit directory to one or more work-item IDs.

Configuration validation rejects unknown schema versions, invalid URLs, empty identifiers, duplicate mappings, and paths outside the repository. No token is loaded until a user invokes an online operation. Azure DevOps Services hosts are allowlisted; custom on-premises hosts require an explicit `--allow-custom-host` option and HTTPS.

## Traceability report

- `schemaVersion`: report contract version.
- `featurePath`: normalized repository-relative feature directory.
- `workItems`: observed identifiers, title, type, state, iteration path, source URL, and related links.
- `deliveryReferences`: typed PR, build, and release references with identifier, status when available, URL, and evidence source.
- `diagnostics`: bounded API/configuration warnings and incomplete-analysis state.
- `criteriaDigest`: current digest and baseline status when available.

## PR preview and created draft

- `preview`: title, description, repository, source/target branch inputs, mapped work-item IDs, feature digest, task/evidence summary, traceability links, gaps, and a digest of the saved preview content used to detect edits before submission.
- `createdPullRequest`: populated only from the successful Azure Repos create response; includes PR ID, URL, and `isDraft` state.
- A preview can be written locally as Markdown or JSON. Existing output is protected unless the caller explicitly selects overwrite. A preview never contains credentials.
