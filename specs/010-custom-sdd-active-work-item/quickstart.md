# Quickstart: Configure a Custom SDD Active Work Item

1. Add `activeWorkItemPath` to a custom workflow in `.brownpants/sdd-workflows.json`, for example `planning/changes/add-search`.
2. Ensure that path is below a declared `artifactRoots` entry and contains at least one file in the repository.
3. Run `dotnet run --project src/BrownPants.Cli -- inspect . --format markdown --limit 10` and confirm the custom workflow row names the path with `CustomWorkflowDeclaration` evidence.
4. Add `--format json` and confirm `ActiveWorkItemPath` and `ActiveWorkItemEvidence` match Markdown.
5. Try an outside-root or missing path and confirm BrownPants reports a diagnostic, leaves active fields null, and still reports the workflow's counts.
6. Run `dotnet test BrownPants.sln` for fixture coverage.
