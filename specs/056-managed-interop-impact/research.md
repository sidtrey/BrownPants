# Research: Managed Interop Impact

## Repository Findings

- `CSharpAnalyzer` and `VisualBasicAnalyzer` parse source without MSBuild, but their public member lists are bounded by the requested sample limit.
- `NativeFlowAnalyzer` currently connects MFC maps, handler definitions, and explicitly qualified native calls, but does not inspect C++/CLI handle calls.
- `OrderManagedBridge.cpp` currently contains only a comment where the managed forward is claimed; there is no actual managed call for BrownPants to report.
- Feature 055 can reverse-trace native relations from a changed source path, but currently only accepts native-source seeds for native impact.

## Decisions

- Keep an internal bounded method-target index in the existing C#/VB syntax passes so normal report sampling remains unchanged.
- Support locally declared C++/CLI managed handles and `->` method calls; do not infer receiver types from arbitrary expressions or runtime DI.
- Match namespace/type/method and expose every possible declaration location. Unique matches remain candidates; overloads and duplicates remain ambiguous.
- Reuse native-flow candidates for inventory, evidence, and managed-target reverse impact.
- Convert the Brownfield Bug Lab's claimed forwarding comment into a concrete C++/CLI call to its managed workflow, while keeping the legacy fixture analysis-only.

## Alternatives Considered

- Use MSBuildWorkspace to compile and bind C++/CLI to managed projects: rejected because it introduces platform/toolchain requirements and may execute imported build targets.
- Link on method name alone: rejected because common names such as `Save` or `Import` produce unsafe matches.
- Claim a resolved interop edge from source text: rejected because source matching does not prove assembly references, overload selection, or runtime execution.
