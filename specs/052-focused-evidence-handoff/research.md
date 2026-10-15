# Research: Focused Evidence Handoff

## Decision: Reuse current investigation matching

`RepositoryInvestigator.Investigate` provides case-insensitive literal path and line-content matches, stable repository-relative paths, bounded file reads, binary/oversize skip diagnostics, and test-path metadata. Focused evidence will consume those match records instead of inventing a different clue interpretation.

**Alternatives considered**: Reimplementing string search in the evidence analyzer would duplicate safety limits and risk mismatched behavior. Probabilistic ranking was rejected because it is less deterministic and has no reviewed truth model.

## Decision: Reuse semantic impact graph

`CallImpactAnalyzer.Analyze` starts from source declarations in changed paths and follows compiler-resolved C#/VB callers transitively. Focused evidence will convert these returned relationships to the existing managed-call finding shape and use impact paths to select corresponding test-path candidates.

**Alternatives considered**: Filtering all resolved edges by file alone would miss transitive callers. Matching symbol names lexically could report false edges. Reusing the impact analyzer keeps compiler binding and graph traversal authoritative.

## Decision: Keep focus additive and opt-in

The default `evidence` invocation remains a full inventory and preserves its current JSON bytes. Focus fields and per-finding selection reasons appear only when a clue or changed path is passed. `--semantic` remains an explicit opt-in because MSBuild imports and design-time targets can execute.

**Alternatives considered**: Making all evidence task-focused by default would break existing SDD integrations and remove repository context users may expect. Automatically enabling semantic analysis for changed paths would violate safe-by-default policy.

## Decision: Union selectors, deduplicate facts

Clues and changed paths independently select findings. A finding selected by both appears once and carries both reasons. Every requested selector remains visible with its match count, including unmatched clues and paths.

**Alternatives considered**: Intersection would hide useful findings whenever a bug clue points to a test or legacy file that is not in the managed call graph. Priority-based precedence would conceal why other evidence was excluded.
