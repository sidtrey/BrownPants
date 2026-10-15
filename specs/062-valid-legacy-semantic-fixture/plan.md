# Implementation Plan: Valid Legacy Semantic Fixture

**Branch**: `062-valid-legacy-semantic-fixture` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Correct the Bug Lab's classic .NET Framework project metadata and dependency wiring so semantic inspection evaluates a valid C# project. Preserve the target framework and existing NHibernate version, and report host limitations without fabricating resolved calls.

## Technical Context

**Language/Version**: C#, classic .NET Framework 4.7.2 fixture; .NET 10 CLI
**Primary Dependencies**: NHibernate 5.5.2 PackageReference; MSBuild C# targets
**Storage**: None
**Testing**: BrownPants semantic CLI inspection, managed Bug Lab checks, xUnit suite
**Target Platform**: Windows for legacy project evaluation; default inventory remains cross-platform
**Project Type**: Brownfield example library, analyzed by local CLI
**Performance Goals**: No additional source scan or unbounded output
**Constraints**: Pin NHibernate exactly at 5.5.2 with a lock file; do not claim resolved calls without Roslyn target evidence; imported targets are evaluated only in opt-in semantic mode. The legacy Framework target is Windows-only.
**Scale/Scope**: One old-style project and its verification documentation.

## Constitution Check

- **Evidence First**: Pass. Compiler diagnostics and resolved/unresolved calls remain distinct.
- **Broad, Engine-Neutral Core**: Pass. Change is limited to the realistic legacy example and its evidence.
- **Deterministic and Reviewable Output**: Pass. No output schema or analyzer behavior changes are planned.
- **Safe Inspection by Default**: Pass. Only the existing opt-in semantic mode evaluates MSBuild imports.
- **Specified and Verified Changes**: Pass. Frozen criteria, focused fixture inspection, managed regression command, build and tests provide observable checks.

## Research and Design Decisions

- The project is an old-style MSBuild XML project with `TargetFrameworkVersion` v4.7.2 and `packages.config` pinning NHibernate 5.5.2. It lacks the standard `Microsoft.CSharp.targets` import and an NHibernate assembly reference, causing MSBuildWorkspace to report no `Compile` target and Roslyn to lack NHibernate symbols.
- Use an exact `[5.5.2]` `PackageReference` and lock file so `dotnet restore` places the package in NuGet's standard cache, add an explicit reference to its netstandard2.0 assembly for the classic project, set C# 10 to match its existing source syntax, declare the Windows runtime identifier required for complete workspace evaluation, and import C# targets. Remove the superseded packages.config.
- Keep `OrderEntity` non-sealed because its virtual members are required for NHibernate proxying and cannot compile in a sealed class.
- Keep default inspection unchanged. Semantic mode remains opt-in because imported MSBuild targets can execute.
- Re-run semantic inspection and capture diagnostics after the import. If restore prerequisites prevent NHibernate resolution, record the specific limitation and keep the project-coverage flag conservative; do not add local stubs that would create false compiler relationships.
- No new API, persistent data model, or generated contract is introduced; data-model and contracts artifacts are not applicable.

## Project Structure

```text
examples/brownfield-bug-lab/legacy/Framework/
├── LegacyOrderDesk.csproj
├── OrderDataAccess.cs
└── packages.lock.json
examples/brownfield-bug-lab/README.md
specs/062-valid-legacy-semantic-fixture/
├── spec.md
├── plan.md
├── research.md
├── quickstart.md
├── tasks.md
└── verification.md
```

**Structure Decision**: Keep the classic project format while correcting its language, target imports, and dependency references; record environment-dependent semantic results in feature verification.

## Risks and Mitigations

- MSBuild evaluation may still report unresolved external types when legacy NuGet packages are not restored. Preserve the unresolved evidence and incomplete state.
- Project evaluation can run imported build targets. This remains restricted to documented semantic commands on the trusted checkout.

## Complexity Tracking

No constitution violations or new runtime dependencies.
