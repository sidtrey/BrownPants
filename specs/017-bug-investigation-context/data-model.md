# Investigation Report Data Model

## InvestigationReport

- `SchemaVersion`: Integer identifying this report contract, initially `1`.
- `Root`: Absolute inspected repository root, consistent with existing reports.
- `Clues`: Ordered user-provided clues, each with its own count and bounded matches.
- `ReadIssueCount`: Total files skipped or unreadable.
- `ReadIssues`: Bounded repository-relative paths and stable issue codes for skipped or unreadable files.
- `SampleLimit`: Maximum number of records emitted per clue.
- At most 20 clues are accepted, each no longer than 1000 characters.

## ClueResult

- `Clue`: Exact supplied non-empty string, retained for traceability.
- `MatchCount`: Total path and line matches before sampling.
- `Matches`: Stable, bounded list of `InvestigationMatch` records.
- A line contributes at most one content match per clue, even when the clue occurs multiple times on that line.

## InvestigationMatch

- `Kind`: `Path` or `Content`.
- `Path`: Repository-relative slash-separated location.
- `Line`: One-based line for content matches; null for path matches.
- `IsTestCandidate`: Whether the path follows existing test-path conventions.

## InvestigationReadIssue

- `Path`: Repository-relative slash-separated path.
- `Code`: Stable diagnostic such as `ReadFailure`, `BinaryFileSkipped`, or `FileTooLarge`.

Matching is ordinal, case-insensitive literal substring matching. The report never stores source-line contents. Binary files and files larger than 10 MiB are skipped for content matching, with bounded read-issue codes; path matches remain available. A file may yield a path match and one or more content matches for a clue.
