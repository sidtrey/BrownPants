# Quickstart: Azure DevOps Traceability and Draft PRs

1. Add `.brownpants/azure-devops.json` with organization, project, repository, authentication scheme, and a mapping from `specs/030-azure-devops-bridge` to an existing work item. Do not put credentials or credential-variable names in this file.
2. Set `BROWNPANTS_AZURE_DEVOPS_TOKEN` using the organization's approved credential process. Prefer a short-lived Microsoft Entra token with read permissions for traceability. PR creation additionally needs Azure Repos permission to contribute PRs. For a custom Azure DevOps Server host, pass `--allow-custom-host` explicitly.
3. Generate a report explicitly:

   ```powershell
   brownpants devops trace . --feature specs/030-azure-devops-bridge --online --format markdown
   ```

4. Prepare a local preview for review:

   ```powershell
   brownpants devops pr preview . --feature specs/030-azure-devops-bridge --source feature/030-azure-devops-bridge --target main --online --output artifacts/030-pr-preview.md
   ```

5. After reviewing the complete preview, submit it as a draft PR with an explicit command:

   ```powershell
   brownpants devops pr create . --preview artifacts/030-pr-preview.md --confirm --online
   ```

The create request associates mapped work items using the Azure Repos PR create API. BrownPants reports the resulting PR link. Review, approval, completion, and merge remain in Azure DevOps under the team's normal controls. No command updates work-item fields or queues builds/releases.
