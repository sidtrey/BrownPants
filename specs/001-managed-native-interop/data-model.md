# Data Model: Managed and Native Interop Analysis

BrownPants emits an in-memory repository inventory; this feature adds no persistent data store. Existing JSON serialization exposes the same records that Markdown summarizes.

## Repository inventory

- **RepositoryInventory**: Root, scanned file count, extension counts, project markers, test-path signals, top-level entries, Spec Kit state, language analyses, architecture signals, and .NET/native project metadata.
- **DotNetProjectInfo**: Repository-relative project path, target frameworks, language versions, project references, legacy-framework indicator, and read error when applicable.
- **NativeProjectInfo**: Repository-relative native project path, MFC and CLR indicators, target frameworks, toolsets, project references, and read error.

## Source analysis

- **CSharpAnalysis / VisualBasicAnalysis**: File, type, member, and invocation counts; bounded source samples; parse issue samples. C# also reports using directives, inactive regions, and optional semantic results.
- **CSharpSemanticAnalysis**: Discovered and loaded project counts, resolved and unresolved invocation counts, call samples, candidate targets, and workspace issues. A resolved edge requires a Roslyn symbol result in the loaded configuration.
- **ArchitecturePatternAnalysis**: Total signal count and bounded `ArchitecturePatternSignal` samples.
- **ArchitecturePatternSignal**: Category, evidence kind, source language, repository-relative path, one-based line, and source evidence. Current pattern signals use the `Candidate` evidence kind and do not prove runtime behavior.

## Validation and limits

Paths are relative to the inspected repository; line numbers are one-based when source spans are available. Sample lists may be capped while total counts remain available. Read and parse issues must remain visible. Native lexical evidence cannot resolve macro expansion, dynamic loading, or arbitrary native call relationships.
