# Implementation Plan: Measurable Brownfield Capability Demo

## Summary

Add a compact, package-free showcase under `examples/brownfield-bug-lab/`. Its managed .NET 10 sample will contain tricky generic/overloaded call paths, a factory, and DI wiring that Roslyn can load. Separate evidence-only legacy files will show .NET Framework metadata, MFC message-map/COM/C++/CLI patterns, and NHibernate packages/mappings/configuration. A seeded regression will be applied only to temporary comparison copies.

## Repository Evidence

- `CallImpactAnalyzer` uses `CSharpSemanticAnalyzer` and currently resolves C# and VB method calls only. It reports completeness limits for unsupported/unmapped inputs.
- Existing `CallImpactAnalyzerTests.SemanticAnalyzer_MapsOverloads...` verifies distinct overload target mapping.
- `ArchitecturePatternAnalyzer` reports source-local DI/factory/COM/MFC/NHibernate signals as candidates; `ArchitecturePatternAnalyzerTests` and `NHibernateAwarenessTests` check those categories and noise filtering.
- Do not compile the native MFC or evidence-only .NET Framework project in the portable demo checks. Run semantic call impact against the isolated managed project so unsupported legacy projects do not contaminate its completeness status.

## Demo Design

- `examples/brownfield-bug-lab/managed/`: package-free SDK-style .NET project, typed generic service/store, overloaded methods, decoy same-name call, explicit factory/DI registration, and console regression checks.
- `examples/brownfield-bug-lab/legacy/`: analysis-only .NET Framework project metadata, MFC view/message map, C++/CLI or COM boundary pattern, NHibernate package/configuration/mapping examples, and carefully marked decoys.
- `examples/brownfield-bug-lab/evaluation/`: frozen bug report, prompt, reviewed answer key (not included in agent prompt), baseline/assisted result schema, and seed/fix patch or scripts.
- `scripts/`: deterministic isolated-copy runner and scoring script. Only BrownPants evidence is supplied to the assisted agent; all other run inputs stay byte-identical.
- Publish the measured run in a dedicated comparison document and a short README scenario only after the score threshold is achieved. If it is not, publish the result without an advantage claim and revise the task or fixture.

## Validation

Build and run the package-free managed checks. Run BrownPants inspect/architecture analysis against the full sample; run semantic impact against `managed/` only. Verify report findings against the answer key and decoys. Exercise the seed in disposable copies, confirm the same pre-fix regression, and run identical post-fix checks in both copies. Validate hashes and evidence ledger before publication.
