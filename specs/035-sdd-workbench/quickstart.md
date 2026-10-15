# Quickstart: Local SDD Review Workbench

From a repository with one active Spec Kit feature:

```powershell
brownpants workbench .
```

Open `TestResults/brownpants-workbench.html` in a browser. To select a different feature or output location:

```powershell
brownpants workbench . --feature specs/012-example --output TestResults/review.html
```

Reviewers can add comments to stories, criteria, or tasks and export a JSON handoff from the page. Share that handoff with the feature owner; the original feature artifacts are unchanged. The page works as a local file without a server or network access.
