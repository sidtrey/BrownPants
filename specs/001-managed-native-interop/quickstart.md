# Quickstart: Validate Managed and Native Interop Analysis

## Prerequisites

- .NET 10 SDK.
- A checkout of BrownPants.
- A target repository or a small fixture with `.cs`, `.vb`, `.cpp`/`.h`, `.csproj`/`.vbproj`, and `.vcxproj` files.

## Build and test

```powershell
dotnet build BrownPants.sln
dotnet test BrownPants.sln
```

Expected result: build succeeds and xUnit fixtures pass, including tests for legacy project parsing, C#/VB syntax, MFC metadata and signals, DI/factory patterns, source noise, JSON output, and deterministic reports.

## Inspect a repository

```powershell
dotnet run --project src/BrownPants.Cli -- inspect C:\path\to\legacy-repo
dotnet run --project src/BrownPants.Cli -- inspect C:\path\to\legacy-repo --format json --limit 1000
```

Review project markers and native MFC/CLR metadata, then inspect architecture pattern signals at their cited paths and lines. Treat native C++ matches as candidates and verify both sides of each managed/native boundary.

For symbol-resolved managed call edges only when project files are trusted and required build references are available:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect C:\path\to\legacy-repo --semantic
```

Expected result: project-load counts, resolved/unresolved calls, and load issues appear. A failed or partial project load limits semantic coverage; the default static inventory remains the baseline.

## Try the Codex + Spec Kit workflow

Initialize a Spec Kit project with Codex skills, run `brownpants inspect .`, then invoke `$speckit-specify` followed by `$speckit-plan`. BrownPants should ground the resulting spec and plan in cited repository evidence. The installed Spec Kit command skills are available under `.agents/skills`; the BrownPants extension can provide its context command when its manifest is built and installed.
