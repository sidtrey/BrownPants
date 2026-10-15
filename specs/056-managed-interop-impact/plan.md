# Implementation Plan: Managed Interop Impact

**Branch**: `056-managed-interop-impact` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Add source-only C++/CLI-to-managed call candidates from explicitly typed handle calls, preserve every possible managed target location, include the cross-boundary relation in inventory and evidence, and extend reverse impact from changed C#/VB targets back through bridge and MFC candidates.

## Technical Context

**Language/Version**: C# / .NET 10; analyze C++, C++/CLI, C#, and VB source
**Primary Dependencies**: Existing Roslyn C# and VB syntax packages; no new runtime package
**Storage**: None
**Testing**: xUnit temporary mixed-language fixtures, Brownfield Bug Lab, packed CLI smoke
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local CLI analyzer
**Performance Goals**: Linear source indexing plus bounded relation traversal and target samples
**Constraints**: No MSBuild/native build needed for candidate discovery; no execution or source writes; all cross-language edges remain lexical candidates
**Scale/Scope**: Locally declared C++/CLI managed handles, instance calls, and unique/ambiguous C#/VB declarations

## Constitution Check

- **Evidence First**: Pass. Links identify candidate source declarations only; overloads, unresolved targets, project references, and runtime behavior remain unproven.
- **Broad, Engine-Neutral Core**: Pass. Inventory, impact, and evidence are not tied to an SDD host.
- **Deterministic and Reviewable Output**: Pass. Preserve target candidate locations, stable IDs/order, source-relative paths, and bounds.
- **Safe Inspection by Default**: Pass. Roslyn syntax parsing does not load projects; native source remains read-only.
- **Specified and Verified Changes**: Pass. Frozen criteria, decoy and ambiguity fixtures, package smoke, and evidence ledger.

## Design

Extend the syntax passes to retain a bounded internal index of C# and VB method declarations while keeping existing report samples unchanged. Extend native-flow extraction to find local `Type^ receiver` declarations and `receiver->Method(...)` calls inside C++/CLI method bodies. Match namespace/type/method against the managed declaration index; use unique, unresolved, or ambiguous status and preserve all bounded target locations. A method name alone or an untyped receiver is insufficient for a unique edge.

Represent cross-language calls as `cppcli-managed-call` native-flow relations. Expose the candidate data in inventory and engine-neutral evidence. When a changed managed path is a unique or possible target, seed native reverse impact from that relation, then traverse qualified native calls and MFC dispatch. Keep these findings in the native candidate collection, separate from Roslyn-managed call edges. If the target index or relation list is truncated, surface the omission and mark impact incomplete.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── CSharpAnalyzer.cs              # bounded internal C# method index
├── VisualBasicAnalyzer.cs         # bounded internal VB method index
├── RepositoryInventory.cs         # method-target and candidate-target models
├── ArchitecturePatternAnalyzer.cs # pass managed index into native flow
├── NativeFlowAnalyzer.cs          # handle-call extraction and managed matching
├── EvidenceReport.cs              # candidate target locations in handoff
├── EvidenceFocusAnalyzer.cs       # changed managed path -> native impact selection
└── CallImpactAnalyzer.cs          # cross-language reverse traversal
tests/BrownPants.Tests/
├── RepositoryScannerTests.cs
├── CallImpactAnalyzerTests.cs
└── EvidenceCommandTests.cs
tests/PackedManagedInteropSmoke.ps1
examples/brownfield-bug-lab/legacy/Bridge/OrderManagedBridge.cpp
specs/056-managed-interop-impact/
```

**Structure Decision**: Reuse the existing Roslyn syntax passes and native-flow report pipeline; do not add a compiler or project-evaluation step.

## Risks and Mitigations

- Same names across namespaces can produce false links: use `using namespace` or fully qualified namespace context when present; otherwise preserve ambiguity.
- Overloads and partial declarations can duplicate targets: expose every candidate location and never choose one.
- Generic construction and inheritance complicate binding: match the explicit handle's declared type and method name only; state that compiler binding is not proven.
- Target indexes and flow relations are bounded: retain explicit counts and incomplete status when truncation could hide candidates.
- Regex-only C++/CLI extraction can miss valid syntax: keep supported forms narrow and include negative fixtures; report scope as a limit.

## Research

Pre-implementation baseline: BrownPants parsed discovered C# and VB files without building projects, but their structured type/member arrays were sampled by `--limit`. The Bug Lab bridge body was only a comment claiming a managed forward, so its evidence could not support a C# target edge. Native flow records already carried candidate status, call sites, target locations, and deterministic IDs; the implementation extends that model with bounded managed method targets and reverse tracing from changed C#/VB paths.

## Complexity Tracking

No constitution violations. Cross-language edges remain syntax candidates and cannot establish project-reference or runtime linkage.
