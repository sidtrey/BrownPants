# Focused Evidence JSON Contract

Command form:

```text
brownpants evidence [repository] [--format json] [--limit 1..10000]
  [--clue <literal> ...] [--changed <relative-cs-or-vb-path> ...] [--semantic]
```

Provide at most 20 values for each selector type; each clue is limited to 1000 characters. The finding and clue-location samples use `--limit`.

At least one `--clue` or `--changed` selector activates focused mode. Selectors use a deterministic union: findings matching any clue or changed-path scope are retained once. `matchedBy` explains each applicable selector and matching reason. Requested but unmatched selectors remain in the focus summary. `--changed` without `--semantic` selects source-local candidate facts and reports caller impact as not analyzed. With semantic mode, existing direct/transitive caller results are included and retain resolved relationship status.

Focused responses retain the top-level `schemaVersion`, repository `root`, project/language `context`, `findings`, `completeness`, and `limitations` fields. They add `focus` and per-finding `matchedBy`. `focus` contains selector results, semantic-impact state, unique selected count, emitted count, and omitted count. Output findings sort using the current evidence ordering; `matchedBy` is an ordinally sorted list. Clue reasons are `path-text`, `evidence-text`, `path-match`, `content-line`, or `content-file` (for file-level test candidates); changed-path reasons are `source-file`, `caller-impact`, or `test-impact`. Counts reconcile: `totalMatchedFindingCount = emittedFindingCount + omittedFindingCount`.

Without selectors, the command emits the existing response with no focus-specific fields. All findings retain `candidate` or `resolved`; selection never changes evidence strength. No matched source-line text is added to clue records.
