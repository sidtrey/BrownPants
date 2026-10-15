# Data Model: Transitive Managed-to-Native Impact

## NativeCallImpact

An existing source-located native candidate record returned by change-impact analysis.

- `ChangedPath`: requested changed file that initiated the analysis.
- `RelationshipId`, `RelationshipKind`, `Status`: identity and native evidence classification.
- `SourceSymbol`, `SourcePath`, `SourceLine`: upstream candidate caller and source location.
- `TargetSymbol`, `TargetPath`, `TargetLine`, `CandidateTargets`: target evidence from native-flow analysis.
- `Depth`: native reverse-trace edge distance, unchanged.
- `ManagedImpactDepth`: nullable managed caller depth associated with the exact declaration that seeded this trace; null for direct path seeds.
- `EvidenceKind`: remains `native-lexical`.

The combination of changed path and relationship identity deduplicates a relation. If several managed paths reach one relation, retain the smallest managed depth deterministically. Candidate targets remain candidates and may have multiple locations.
