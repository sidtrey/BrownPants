# Quickstart: Task-Based Model Guidance

1. Copy [`examples/model-routing-policy.json`](../../examples/model-routing-policy.json) to `.brownpants/models.json` and replace model names with identifiers available to your team.
2. Assign each profile one or more categories and a tier (`economy`, `balanced`, or `high`). Model IDs and tiers are team choices; BrownPants does not rank or verify providers.
3. Validate and review the mapping:

   ```powershell
   dotnet run --project src/BrownPants.Cli -- models validate . --policy examples/model-routing-policy.json
   dotnet run --project src/BrownPants.Cli -- models report . --format markdown
   dotnet run --project src/BrownPants.Cli -- models report . --format json
   ```

4. Explicitly export only the host the project uses:

   ```powershell
   dotnet run --project src/BrownPants.Cli -- models export . --host vscode
   dotnet run --project src/BrownPants.Cli -- models export . --host visual-studio
   dotnet run --project src/BrownPants.Cli -- models export . --host claude
   dotnet run --project src/BrownPants.Cli -- models export . --host codex
   ```

Exports create project-local agent definitions. Visual Studio requires version 18.4 or later; its agent files use a distinct filename under the same `.github/agents/` directory as VS Code. A user or host must select or invoke the corresponding agent. Export does not configure credentials, test model availability, force delegation, or change host-global settings. Conflicting files are preserved; edit or remove them deliberately before re-exporting.
