# Data Model: Focused Evidence

## Focus request

- `clues`: ordered input literal strings; non-empty when supplied; validated with existing investigator bounds.
- `changedPaths`: at most 20 ordered repository-relative C# or VB source paths; normalized and validated under the repository root.
- `semanticRequested`: the existing `--semantic` choice, default false.
- `sampleLimit`: existing bounded output limit.

## Focus summary

- `clues`: each input clue, number of matching repository locations, and whether it matched.
- `changedPaths`: each normalized path, selected source findings, impact relationships, and validation/analysis state.
- `semanticImpactAnalyzed`: true only when changed paths and semantic analysis produced an impact graph.
- `totalMatchedFindingCount`: number of unique findings selected before output truncation.
- `emittedFindingCount`: number serialized.
- `omittedFindingCount`: selected facts omitted due to the sample limit.

## Focused finding

Retains the existing `EvidenceFinding` identity, category, status, signal, path, source line, signatures, and evidence. Optional `matchedBy` lists deterministic selector references such as `clue:<input-index>` or `changed-path:<normalized-path>` plus a reason (`path-text`, `evidence-text`, `path-match`, `content-line`, `content-file`, `source-file`, `caller-impact`, or `test-impact`). Distinct reasons do not duplicate the finding.

When focus is absent, focus-specific JSON fields are omitted and existing finding serialization remains unchanged.
