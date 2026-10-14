# CLI Contract: `investigate`

## Invocation

```text
brownpants investigate [repository-path] --clue <text> [--clue <text> ...] [--format markdown|json] [--limit 1..10000]
```

- Repository path is optional and defaults to the current directory.
- At least one non-empty `--clue` is required. Each value is matched as a literal, case-insensitive substring; values are not tokenized or interpreted as regular expressions.
- At most 20 clues are accepted, each no longer than 1000 characters.
- `--format` defaults to `markdown`; supported values are `markdown` and `json`.
- `--limit` defaults to 200 and caps emitted matches independently for each clue. Counts still reflect all matches.
- Exit `0` indicates a report was produced, including when clues have no matches. Exit `1` indicates the repository could not be inspected. Exit `2` indicates invalid command-line input.

## JSON contract, version 1

The root object contains `SchemaVersion`, `Root`, `Clues`, `ReadIssueCount`, `ReadIssues`, and `SampleLimit`. Each clue result contains the original `Clue`, total `MatchCount`, and ordered `Matches`. Each match contains `Kind`, repository-relative `Path`, nullable one-based `Line`, and `IsTestCandidate`. `Path` matches have `Line: null`; `Content` matches have a positive line number. Each read issue contains a path and stable code; total issue count is retained when samples are truncated. No matched source contents are emitted.

## Markdown contract

Markdown presents each clue, total and omitted counts, matched paths and lines, test-candidate labels, read issues, and the evidence limit: literal matches are leads, not proof of causality, behavior, or regression coverage.

## Safety and ordering

The command reads files discovered under the repository root only; it skips the same ignored directories and symlinks as `inspect`. Files over 10 MiB and files containing NUL characters are skipped for content matching and reported as issues, but can still match by path. Clues are reported in argument order; matches are ordered by repository path and then line, with path match before content matches for that path. Duplicate clues remain distinct inputs so their provenance is preserved.
