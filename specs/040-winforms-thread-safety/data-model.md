# Data Model: WinForms Cross-Thread UI Access Candidates

## UI access candidate

- `path`: repository-relative C# or Visual Basic source path.
- `line`, `column`: one-based location of the property assignment.
- `receiver`: syntax name for the property receiver (no source line/body text).
- `property`: property name written by the assignment.
- `controlTypeEvidence`: `WinFormsTypeName`, `SourceDerivedControl`, or a bounded syntax-recognition description.
- `executionContext`: `KnownBackgroundCallback` or `Unknown`.
- `backgroundEvidence`: recognized callback syntax, when present.
- `marshaling`: `DispatcherCallbackObserved`, `InvokeRequiredMarkerObserved`, `NoMarkerObserved`, or both marker/dispatch evidence where applicable.
- `classification`: always `Candidate`; no `Safe` or `ConfirmedDefect` state exists.

## Report

- `schemaVersion`: stable JSON schema version.
- `filesScanned`: C#/VB source files considered.
- `assignmentCount`: recognized Control property writes before limiting.
- `findings`: deterministic path/line/column order, capped by `--limit`.
- `omittedFindingCount`: candidates omitted by the bound.
- `parseErrorCount`, `parseErrors`: total and bounded source-located Roslyn diagnostics.
- `coverageNotes`: concise limits for unsupported aliases, inferred types, or indirect callbacks when observed.

Source strings and matched assignment text are never fields in the report.
