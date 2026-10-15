# Research: Grouped Candidate Evidence

## Decision

Expose `repeatedCandidateGroups` as an additive roll-up of emitted architecture/framework candidate rows with identical category, signal, and normalized path. Each group links existing finding IDs and sorted line numbers. Keep all original `findings` unchanged. Recompute the roll-up after focus selection.

## Rationale

The v3 benchmark result contains 17 framework candidate rows for 10 reviewed candidate groups. Three excess rows repeat valid MFC message-map, NHibernate session, and transaction locations already represented by the same key in the answer key. The result schema allowed only a singular `line`, despite the key's multi-line groups. Group roll-ups can make these repeated locations easier to report once while preserving per-line verification.

## Alternatives considered

- **Collapse `findings` into grouped records**: rejected because existing consumers rely on per-location IDs and evidence text.
- **Group managed calls or native-flow relations by path**: rejected because distinct callers, endpoints, or dispatch targets must remain separate relationships.
- **Overwrite the v3 answer key or change task acceptance criteria**: rejected. The v3 key's source line is stale for the current fixture and its recorded source commit cannot be recovered. Preserve the v3 artifacts; create a separately reviewed and hashed v4 key from current source, while keeping the bug task and its acceptance hash unchanged.
- **Treat every grouped signal as relevant to the bug**: rejected; grouping is navigation only and does not rank relevance or prove causality.

## Repository evidence

`EvidenceReportAnalyzer.Create` deduplicates and bounds source-located findings. `EvidenceFocusAnalyzer.Select` filters those findings and adds `MatchedBy` reasons. The v3 result key stores `lines` arrays for repeated architecture/framework candidates, while `result.schema.md` and `task-prompt-v3.md` use one `line` per finding. The scorer counts each extra location as a false positive. BrownPants self-analysis found five repeated candidate groups in the bug-lab and no truncation at limit 10,000.
