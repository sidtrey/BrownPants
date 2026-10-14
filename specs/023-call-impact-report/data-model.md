# Data Model: Call-Trace Change Impact Report

## Impact report

- `SchemaVersion`: report contract version.
- `ChangedPaths`: validated repository-relative input paths.
- `IsComplete`: whether all known projects, resolved edges, and source mappings were available within configured bounds.
- `Summary`: unique direct/transitive callers, affected files/projects, test-candidate call sites, and maximum caller depth.
- `Impacts`: root changed method, immediate target at each call-chain step, affected caller symbol, shortest depth, call-site path/line, project, and test-candidate flag.
- `Issues`: partial-load, unresolved-call, unsupported-input, truncation, or unmapped-declaration diagnostics.

## Call edge

A resolved edge links a caller symbol to a target symbol and stores the invocation site's project/path/line plus the target declaration path/line when available. Symbol identity includes containing type, method name, generic arguments, and parameter types to distinguish overloads.

## Completeness rules

A zero-caller result is complete only when requested paths map to loaded source declarations, all detected managed projects load, no unresolved invocations or workspace failures were observed, and call/declaration samples were not truncated. Complete means complete for the loaded static C#/VB project graph, not for reflection, dynamic behavior, native code, or runtime conditions.
