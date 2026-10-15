# Quickstart: OpenSpec Review Workbench

From an OpenSpec repository with one non-archived change:

```powershell
brownpants workbench . --engine openspec
```

Open `TestResults/brownpants-workbench.html` in a browser. If more than one active change exists, select one explicitly:

```powershell
brownpants workbench . --engine openspec --feature openspec/changes/add-checkout --output TestResults/checkout-review.html
```

Review the proposal summary, requirement/scenario headings, tasks, design, and original artifacts. Add feedback to a review point and download `brownpants-review-notes.json` to hand to the change owner. The report and note export work from local files without network access; they do not validate, edit, or archive the OpenSpec change.
