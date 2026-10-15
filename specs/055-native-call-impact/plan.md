# Implementation Plan: Native Call Impact

**Branch**: `055-native-call-impact` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Extend the existing impact command to accept native C/C++ paths, trace reverse MFC and qualified-call candidate relations, and report them separately from Roslyn-resolved managed impact. Reuse the native-flow inventory and preserve its ambiguity, omission, and lexical limits.

## Technical Context

**Language/Version**: C# / .NET 10; scanned C, C++, C++/CLI, C#, and VB source
**Primary Dependencies**: Existing .NET/Roslyn dependencies; no new runtime packages
**Storage**: None
**Testing**: xUnit temporary repositories, Brownfield Bug Lab CLI checks, packed CLI smoke
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local CLI analyzer
**Performance Goals**: Bounded graph traversal over discovered native-flow relations and existing managed call edges
**Constraints**: Local, read-only analysis; managed semantic mode already loads MSBuild projects; native analysis remains lexical and source-only
**Scale/Scope**: Multi-path impact for discovered C#/VB and native source files; no new parser or runtime evidence

## Constitution Check

- **Evidence First**: Pass. Native edges stay lexical candidates; unresolved, ambiguous, and omitted links lower completeness.
- **Broad, Engine-Neutral Core**: Pass. The existing `impact` CLI contract is available independent of an SDD engine.
- **Deterministic and Reviewable Output**: Pass. Preserve stable ordering, repository-relative paths, bounded reports, and evidence-specific labels.
- **Safe Inspection by Default**: Pass with existing managed semantic behavior retained; native scanning reads source only and executes no native tools.
- **Specified and Verified Changes**: Pass. Frozen AC/SC criteria, fixture tests, packed smoke, and acceptance evidence ledger are required.

## Design

`RepositoryScanner.Scan` already produces Roslyn semantic calls and `ArchitecturePatternAnalysis.NativeFlow`. Extend `CallImpactAnalyzer` to validate supported changed extensions and analyze both relationship sources. For native changes, use relation target locations/symbols to seed reverse traversal; connect qualified-call source symbols to prior MFC-dispatch target symbols. Include only uniquely linked candidate edges as path relations, retain nearby unresolved/ambiguous evidence as completeness issues, and guard cycles with visited relation/symbol identities. Keep native and managed report collections or explicit evidence kinds separate so renderers never imply equal confidence.

Update CLI help and Markdown rendering while keeping existing JSON fields additive and preserving managed-only output semantics. For native-only requests, skip MSBuild semantic loading; mixed requests keep it explicit and report managed and native status independently in separate collections. The generic evidence handoff should accept native changed paths and select native-flow findings; it must continue labeling them as candidates. Validate relative paths against discovery and reject traversal, missing files, and unsupported types. Honor native-flow omitted counts and caller sample limits in completeness.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── CallImpactAnalyzer.cs       # managed and native reverse traversal
├── EvidenceFocusAnalyzer.cs    # changed-path selection and native evidence
├── RepositoryInventory.cs      # existing impact/native evidence records
└── NativeFlowAnalyzer.cs       # existing lexical relation source
src/BrownPants.Cli/Output/
└── CallImpactReportRenderer.cs # distinct managed/native sections and limits
src/BrownPants.Cli/Program.cs   # impact command validation/help
src/BrownPants.Cli/EvidenceCommand.cs
tests/BrownPants.Tests/
├── CallImpactAnalyzerTests.cs
├── EvidenceCommandTests.cs
└── RepositoryScannerTests.cs
tests/PackedNativeImpactSmoke.ps1
specs/055-native-call-impact/
```

**Structure Decision**: Extend the current CLI analysis pipeline and its report; avoid a separate native graph model unless required to keep evidence categories explicit.

## Risks and Mitigations

- Lexical identity can conflate overloads: include only unique candidate relations and preserve ambiguity as incomplete evidence.
- Relation samples can hide callers: propagate `OmittedCount` and bounded-report notices.
- Mixed-language rendering can overstate certainty: label every native relation `candidate` and list analyzer limits.
- A changed file may be both a caller and target: use visited identities and deterministic deduplication.

## Research

Repository evidence shows `NativeFlowRelation` already stores relation kind/status, source and target paths/lines/symbols, and candidate counts. `CallImpactAnalyzer` currently accepts only `.cs` and `.vb`, while `EvidenceCommand.ValidateChangedPaths` has the same restriction. The Brownfield Bug Lab reports exactly two relevant native candidates: `ON_COMMAND` → `COrderMfcView::OnImportBatch` and that handler → `COrderManagedBridge::ImportSelectedOrders`. No external package or compiler integration is needed.

## Complexity Tracking

No constitution violations. The implementation will not promote source-local native relations to compiler-resolved calls.
