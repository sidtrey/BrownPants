# Quickstart: Verify Active SDD Work Item Reporting

From the repository root:

1. Run `dotnet test BrownPants.sln` to exercise temporary repository fixtures.
2. Run `dotnet run --project src/BrownPants.Cli -- inspect . --format markdown --limit 10` and confirm the Spec Kit active path comes from `.specify/feature.json`.
3. Run the same command with `--format json` and confirm `SddWorkflows` exposes matching `ActiveWorkItemPath` and `ActiveWorkItemEvidence` values.
4. Confirm that a unique OpenSpec change is selected, while multiple changes and invalid Spec Kit pointers have no active path.

The current repository pointer is local ignored state; inspect it before interpreting the reported Spec Kit selection.
