# Implementation Plan: C# Consumption of C++/CLI Libraries

**Branch**: `feature/074-csharp-cli-consumption-impl` | **Date**: 2026-11-12 | **Spec**: [spec.md](spec.md)

## Summary

Add a C#-first, source-only candidate path across legacy managed project references into C++/CLI wrappers and their supported qualified native calls. Reuse the current C# syntax pass, safe XML project readers, and native-flow report/impact structures. Keep new relationships distinct from compiler-resolved Roslyn calls and from the existing C++/CLI-to-managed candidate direction.

## Technical Context

**Language/Version**: C# / .NET 10; analyze C#, legacy MSBuild project XML, C++/CLI, and C++ source
**Primary Dependencies**: Existing Roslyn syntax packages and `System.Xml.Linq`; no new runtime package
**Storage**: None
**Testing**: xUnit temporary repository fixtures, a pinned upstream source scenario, full solution suite, CLI Markdown/JSON and focused impact checks
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local CLI analyzer
**Performance Goals**: Linear source/project indexing with bounded call and target reporting
**Constraints**: No MSBuild evaluation/build, native compiler, binary loading, network, or source writes; path references stay within discovered repository content
**Scale/Scope**: C# local receiver types and invocation syntax; legacy assembly `Reference`/`HintPath` and project references; discovered CLR-enabled native projects and supported wrapper/native method forms

## Constitution Check

- **Evidence First**: Pass. The relation is a source/reference-backed candidate, never compiler binding, assembly loading, or runtime execution.
- **Broad, Engine-Neutral Core**: Pass. The data is available through core inventory, evidence, and impact regardless of Spec Kit or another SDD host.
- **Deterministic and Reviewable Output**: Pass. IDs, sorting, source locations, samples, and omission counts are stable and bounded.
- **Safe Inspection by Default**: Pass. Project metadata is parsed as bounded XML without evaluation; referenced paths are constrained to repository files.
- **Specified and Verified Changes**: Pass. The pinned VCSamples evidence, decoy/ambiguity fixtures, CLI assertions, build, suite, and documentation cover the changed path.

## Design

1. Extend the legacy .NET project reader to retain assembly reference identity and `HintPath` in addition to `ProjectReference` metadata. Extend native-project metadata with output assembly identity needed to associate a C# reference with a discovered CLR-enabled project. Keep configuration-conditioned or absent output information uncertain.
2. Extend the bounded C# syntax index with source-located invocation candidates and the narrow local receiver-type facts needed for explicit variable types and local object creation. Do not create a second C# parser or treat an expression name as proof of a type.
3. Identify CLR-enabled C++/CLI wrapper types/methods in discovered native source and associate them with a C# project only when project/reference metadata and method/type evidence agree. Use a new relation kind for the managed-consumer direction; preserve all candidate targets and statuses.
4. Include these records in inventory and focused evidence. Extend impact so a changed C# consumer path follows the outgoing candidate edge into the wrapper and then through supported qualified native calls. Existing incoming impact and Roslyn-managed paths remain unchanged.
5. Report non-evaluated project conditions, missing binaries, unresolved wrapper sources, truncation, and unsupported call forms. Markdown and JSON use candidate wording and stable locations.

## Project Structure

```text
src/BrownPants.Cli/Analysis/
├── DotNetProjectAnalyzer.cs          # legacy references and HintPath metadata
├── NativeProjectAnalyzer.cs          # native output assembly identity
├── RepositoryInventory.cs            # bounded call/reference/relation models
├── CSharpAnalyzer.cs                 # call sites and local receiver-type facts
├── NativeFlowAnalyzer.cs              # identify wrapper methods and relate consumers
├── ArchitecturePatternAnalyzer.cs   # pass the project/source indexes
├── EvidenceReport.cs                 # managed-consumer candidates and match reasons
├── EvidenceFocusAnalyzer.cs          # focused C# selection and completeness
├── CallImpactAnalyzer.cs             # changed consumer -> wrapper -> native flow
└── Output/MarkdownReport.cs          # readable report and evidence limits
tests/BrownPants.Tests/
├── RepositoryScannerTests.cs
├── EvidenceCommandTests.cs
└── CallImpactAnalyzerTests.cs
tests/PackedManagedInteropSmoke.ps1
examples/brownfield-bug-lab/legacy/
└── ManagedConsumer/                  # small source-only C# -> C++/CLI -> C++ fixture
specs/074-csharp-cli-consumption/
```

**Structure Decision**: Extend the existing source-only scanner and candidate relation pipeline. Do not load the referenced assembly or invoke MSBuild to evaluate the old consumer project.

## Risks and Mitigations

- Old project references are configuration-dependent: report only static metadata and retain conditional/unknown state; do not claim that a reference is active.
- A DLL filename can match multiple native projects: preserve ambiguity; require receiver and wrapper type/method evidence to agree before emitting a unique target.
- Local C# receiver inference is incomplete: initially support explicit local types and local object-creation initializers; report other receiver forms as unsupported rather than guessing.
- Native method parsing is lexical: reuse its comment/string handling and qualified-call limits; keep every interop edge labeled candidate.
- Traversal can mix C#->wrapper and existing native->managed direction: use an explicit relation kind and test both directions to prevent changed-path regressions.
- Large repositories can truncate references or source indexes: use explicit retained/omitted counts and set focused impact incomplete when relevant indexes are truncated.

## Research

See [research.md](research.md) for the pinned VCSamples paths and existing analyzer boundaries.

## Complexity Tracking

No constitution violations. The added relation direction is needed to close the observed C#-first brownfield navigation gap; it reuses existing bounded source evidence rather than introducing compiler or build execution.
