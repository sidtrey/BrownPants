# Research: Transitive Managed-to-Native Impact

## Decision: Bind seeds to exact impacted declarations

`CallImpactAnalyzer.Analyze` already computes managed caller impacts with fully qualified caller identity, path, line, and managed depth. `CSharpSemanticAnalysis.Declarations` maps symbols to path and declaration line. `NativeFlowRelation` exposes exact target locations and candidate target locations. Join on exact path and line plus normalized method identity: Roslyn symbols include `global::` and parameter signatures while native candidate symbols use a shorter source-based spelling. File-only or line-only matching would over-select unrelated methods.

**Alternatives considered**: Add native scanning to the managed traversal (mixes evidence types and graph semantics); seed by path alone (includes unrelated same-file methods); or re-run semantic analysis per caller (unnecessary and expensive).

## Decision: Preserve separate depth dimensions

`NativeCallImpact.Depth` is currently native reverse-trace distance. Keep it unchanged and add nullable `ManagedImpactDepth` for traces seeded from managed caller impact. A null value denotes an existing direct changed-path seed.

**Alternatives considered**: Replace native depth with combined depth (breaking interpretation and existing clients); add a combined depth only (hides native path length); emit duplicate rows for every path (inflates counts).

## Decision: Reuse evidence focus integration

`EvidenceFocusAnalyzer` already selects native findings returned by `CallImpactAnalyzer`; connected native records should therefore flow into focused evidence without broad file-level selection changes. Confirm via CLI integration test.

## Limits

Native flow remains lexical and cannot prove C++ binding, macro expansion, execution, or runtime reachability. Exact source joins do not elevate candidate evidence to compiler-resolved status.
