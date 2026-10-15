# Research: Impact Graph Coverage

## Observed behavior

- `CSharpSemanticAnalyzer.AnalyzeCompilations` increments total resolved counts for each Roslyn-bound invocation and constructor, but calls `AddSample` for each edge using the same limit as semantic report output.
- `CallImpactAnalyzer.Analyze` traverses only `CSharpSemanticAnalysis.ResolvedCalls` and marks the report incomplete whenever that public list is shorter than the total count.
- Pre-implementation baseline: BrownPants-on-BrownPants loaded five projects and retained 817 declarations. The repository contained 11,084 resolved relationships. At `--limit 10,000`, the changed Bug Lab workflow had declarations but no matching caller edges in the retained prefix, so impact reported zero managed callers. The Bug Lab analyzed as its own root reported ten managed caller findings. Post-implementation self-analysis is recorded in [verification.md](verification.md).
- Native interop impact remained present in both reports. One workspace issue and unresolved invocations also remained visible.

## Decision

Retain a second internal list for impact traversal with a fixed 100,000-edge ceiling. Keep public `ResolvedCalls` sampled exactly as today. When the internal graph is available, impact uses it and compares its retained count with full resolved totals. If the ceiling is exceeded, expose an omitted-edge count to impact completeness. Keep all internal graph fields non-public so serialized inventory remains unchanged.

## Alternatives considered

- Raising the public `--limit` ceiling would change documented CLI bounds and still fail for sufficiently large repositories.
- Traversing the public sample preserves the reproduced false-negative behavior.
- Retaining every edge without a ceiling would make memory depend on unbounded repository input and violate the constitution's bounded-evidence requirement.
- Re-scanning all compilations for each changed method would be more complex and multiply semantic traversal cost.
