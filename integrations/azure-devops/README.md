# Azure DevOps

BrownPants connects a feature's Spec Kit evidence to Azure DevOps work items and delivery records. The integration is opt in and treats the signed in user's Azure DevOps permissions as the limit of its access.

## Access setup

Create `.brownpants/azure-devops.json` in the repository:

```json
{
  "schemaVersion": 1,
  "organizationUrl": "https://dev.azure.com/contoso",
  "project": "Product",
  "repository": "repository-guid",
  "authenticationScheme": "entra-browser",
  "entraClientId": "00000000-0000-0000-0000-000000000000",
  "entraTenantId": "contoso.onmicrosoft.com",
  "features": {
    "specs/030-azure-devops-bridge": ["12345"]
  }
}
```

Register a public client application in the organization's Microsoft Entra tenant and enable system-browser sign-in. Tenant app consent policy may require an administrator to approve the Azure DevOps delegated permission. Browser sign-in applies MFA and Conditional Access and does not grant project access; the user still needs the relevant Azure DevOps project and repository permissions. Azure DevOps Server and custom hosts use the configured headless token path instead of browser auth.

For a controlled headless environment, set `BROWNPANTS_AZURE_DEVOPS_TOKEN` through the organization's secret manager and use `authenticationScheme` `bearer` or `pat`, as appropriate. Do not put tokens in this file, command arguments, or committed files. PATs should be narrowly scoped and short lived.

## Commands

Run read-only traceability with explicit online consent:

```powershell
brownpants devops trace . --feature specs/030-azure-devops-bridge --online
```

Create and review a local draft PR preview. This works offline; add `--online` only to include live Azure DevOps links:

```powershell
brownpants devops pr preview . --feature specs/030-azure-devops-bridge --source feature/ado-bridge --target main --output .brownpants/pr-preview.md
```

After reviewing the saved file, explicitly submit it as a draft:

```powershell
brownpants devops pr create . --preview .brownpants/pr-preview.md --online --confirm
```

Creation sends one Azure Repos PR create request with `isDraft: true` and mapped work-item references. BrownPants does not add reviewers, approve, complete, merge, update work-item state, queue builds, or create releases. If the request times out, inspect Azure DevOps before retrying because the service may have accepted it.

## Evidence and limits

The trace report distinguishes observed links from missing or unsupported relations. The preview checks the acceptance-criteria hash, task checklist, evidence ledger, and referenced files. Its QA handoff lists only recorded verification commands and existing evidence, with a visible gap for criteria that lack guidance. Review the report before sharing it; it can include repository paths, work-item details, and delivery URLs.
