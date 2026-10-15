# Implementation Plan: MFC Message Path Evidence

**Branch**: `054-mfc-message-path-evidence` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Add a bounded, source-only native flow index that connects recognized MFC message-map entries to unique handler definitions, then traces explicitly class-qualified calls in those handlers to unique C++/C++CLI definitions. Surface every edge as candidate evidence in inventory and SDD handoff output; preserve unresolved and ambiguous relations.

## Technical Context

**Language/Version**: C# / .NET 10; source evidence scans C++ and C++/CLI lexically
**Primary Dependencies**: Existing .NET and Roslyn packages only; no native parser dependency
**Storage**: None
**Testing**: xUnit temporary mixed-language repositories and packed CLI smoke
**Target Platform**: Windows, Linux, macOS
**Project Type**: Local CLI analyzer
**Performance Goals**: Linear indexing plus bounded relationship lookup for discovered native source files
**Constraints**: No MSBuild evaluation, native build, execution, network access, source writes, semantic claims, or new runtime dependencies
**Scale/Scope**: Recognized MFC map macros, class-qualified method definitions, and explicitly class-qualified calls; incomplete C++ grammar remains unsupported

## Constitution Check

- **Evidence First**: Pass. Native relationships are candidates; duplicate or missing targets remain explicit and are never promoted to compiler-resolved calls.
- **Broad, Engine-Neutral Core**: Pass. Output appears in repository inspection and the generic JSON evidence handoff; it requires no SDD engine.
- **Deterministic and Reviewable Output**: Pass. Use repository-relative source locations, stable sorting, bounded samples, counts, and stated lexical limits.
- **Safe Inspection by Default**: Pass. Read discovered files only; do not invoke a compiler, MSBuild, macros, or external service.
- **Specified and Verified Changes**: Pass. Frozen criteria, temporary fixtures, CLI tests, and packed smoke provide observable evidence.

## Design

The existing architecture scan is source-local. Add a native-flow pass over discovered C/C++ files that sanitizes comments and literals while preserving line positions, indexes class-qualified definitions, extracts bounded MFC message-map entries, and scans linked handler bodies for qualified calls. Link only exact class/method keys with one definition. Keep overload or macro ambiguity visible. Native flow records carry relation kind, source/target symbols and locations, command/event identity where present, status, and evidence text.

Append native-flow records to the existing architecture analysis model and include them in JSON inventory and the evidence report under a distinct `native-flow-candidate` category. Extend Markdown with a readable source-to-target section and coverage summary when native source files were scanned. Keep existing C#/VB Roslyn edges and `impact` behavior unchanged. No relation from C++/CLI into C# is inferred.

### Project Structure

```text
src/BrownPants.Cli/Analysis/
├── ArchitecturePatternAnalyzer.cs # invoke native-flow indexing after source discovery
├── NativeFlowAnalyzer.cs          # bounded sanitizer, map/definition/call linking
├── RepositoryInventory.cs         # native flow records and counts
└── EvidenceReport.cs              # candidate handoff entries and target locations
src/BrownPants.Cli/Output/MarkdownReport.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
tests/PackedNativeFlowSmoke.ps1
examples/brownfield-bug-lab/legacy/Native/
```

**Structure Decision**: Extend the current CLI analysis and report pipeline; keep the native lexer in a focused analyzer and cover it with temporary repositories plus the existing mixed brownfield fixture.

## Risks and Mitigations

- Macro syntax and conditional compilation can vary. Support only documented/common forms and report limits.
- Duplicate overload names may collide under lexical identity. Mark ambiguity instead of selecting a target.
- C++ raw strings and multiline comments can create false matches. Reuse the existing native sanitizer behavior or factor it for shared use; test comments, ordinary/raw strings, and multiline input.
- Inventory JSON changes additively. Preserve schema version and existing fields; validate current consumers and snapshot expectations.

## Research

Microsoft documents MFC message maps as macro entries that associate a message/command with a member handler. `ON_COMMAND` accepts an ID and handler; message-map definitions commonly use `&CMyClass::OnMyCommand`. Sources: [TN006: Message Maps](https://learn.microsoft.com/en-us/cpp/mfc/tn006-message-maps?view=msvc-170), [Message Maps (MFC)](https://learn.microsoft.com/en-us/cpp/mfc/reference/message-maps-mfc?view=msvc-170).

## Complexity Tracking

No constitution violations. The analyzer is lexical by design; adding a C++ compiler front end was rejected because it would require toolchain/project evaluation and would contradict source-only cross-platform inspection.
