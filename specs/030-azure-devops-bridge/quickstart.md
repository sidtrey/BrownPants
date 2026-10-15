# Quickstart: Azure DevOps Traceability and Draft PRs

1. Add `.brownpants/azure-devops.json` with organization, project, repository, authentication scheme, and a mapping from `specs/030-azure-devops-bridge` to its story and related work items. For interactive Services sign-in, use the administrator-provided public client and tenant IDs:

   ```json
   {
     "schemaVersion": 1,
     "organizationUrl": "https://dev.azure.com/your-org",
     "project": "Your Project",
     "repository": "your-repo",
     "authenticationScheme": "entra-browser",
     "entraClientId": "00000000-0000-0000-0000-000000000000",
     "entraTenantId": "your-tenant.onmicrosoft.com",
     "features": { "specs/030-azure-devops-bridge": ["1234", "1235"] }
   }
   ```

   The app registration and delegated Azure DevOps permission may require administrator approval. Browser sign-in cannot add project access the user does not already have. For headless workflows or supported Azure DevOps Server environments, configure `authenticationScheme` as `bearer` or `pat` and set `BROWNPANTS_AZURE_DEVOPS_TOKEN` through the approved credential process. Never store the token in repository config. Pass `--allow-custom-host` for a custom HTTPS Server URL.
2. Generate a report explicitly. This is the point where `--online` starts browser sign-in or reads the environment token:

   ```powershell
   brownpants devops trace . --feature specs/030-azure-devops-bridge --online --format markdown
   ```

3. Prepare a local preview for review. This is offline by default; add `--online` only to include current Azure DevOps delivery links:

   ```powershell
   brownpants devops pr preview . --feature specs/030-azure-devops-bridge --source feature/030-azure-devops-bridge --target main --output artifacts/030-pr-preview.md
   ```

4. After reviewing the complete preview, submit it as a draft PR with an explicit command:

   ```powershell
   brownpants devops pr create . --preview artifacts/030-pr-preview.md --confirm --online
   ```

The create request associates mapped work items using the Azure Repos PR create API. BrownPants reports the resulting PR link. Review, approval, completion, and merge remain in Azure DevOps under the team's normal controls. No command updates work-item fields or queues builds/releases.
