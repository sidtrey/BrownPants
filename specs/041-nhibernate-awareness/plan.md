# Implementation Plan: NHibernate Awareness

**Branch**: `041-nhibernate-awareness` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Extend the existing deterministic architecture-pattern report with NHibernate dependency, mapping/configuration, session/factory, query, transaction, and DI-registration candidates. Use safe XML parsing for direct project/package metadata and Roslyn syntax for C#/VB; do not load assemblies or resolve runtime behavior.

## Technical Context

**Language/Version**: C# / .NET 10; parse C# and VB, inspect XML metadata
**Primary Dependencies**: Existing Roslyn and System.Xml.Linq APIs; no new packages
**Storage**: None; repository files are read-only
**Testing**: xUnit temporary repositories and deterministic output checks
**Target Platform**: Windows, macOS, Linux
**Project Type**: .NET repository analyzer
**Performance Goals**: Linear scan with existing bounded candidate samples
**Constraints**: No MSBuild evaluation, code execution, network, database connection, or file writes; no semantic package resolution
**Scale/Scope**: Existing `inspect` architecture pattern report; C#, VB, `.csproj`/`.vbproj`, `packages.config`, `hibernate.cfg.xml`, and `.hbm.xml`

## Constitution Check

- Evidence First: direct metadata and source syntax remain candidate signals.
- Safe by Default: XML uses DTD-prohibited parsing and no external resolver; no NuGet additions or project evaluation.
- Deterministic: follow existing path ordering, signal limits, and report format.
- Brownfield Compatibility: inspect old .NET Framework project metadata without building it.
- Honest Limits: never infer runtime mapping activation, transitive packages, session lifetime, or database connectivity.

Gate: PASS. Official NHibernate documentation describes `ISessionFactory` as shared/thread-safe and `ISession` as per-work/non-thread-safe, but candidate matching cannot establish whether this repository obeys those practices. [Configuration](https://nhibernate.info/doc/nhibernate-reference/session-configuration.html), [transactions and concurrency](https://nhibernate.info/doc/nhibernate-reference/transactions.html).

## Architecture

1. Extend `ArchitecturePatternAnalyzer` to recognize ORM APIs only when NHibernate namespace/type evidence is present, with conservative receiver/type checks for C# and VB.
2. Parse direct `<PackageReference Include="NHibernate">` and legacy `packages.config` entries as metadata-only candidates.
3. Parse explicitly named `hibernate.cfg.xml` and `.hbm.xml`; also recognize NHibernate mapping/configuration roots in XML without relying on the filename alone.
4. Reuse existing `ArchitecturePatternSignal` categories and bounded sample behavior, adding clear README/report limits.
5. Test positive C#/VB/XML/project metadata cases and negative comments, strings, unrelated `Session`/`Transaction` types, malformed XML, and non-NHibernate package names.

## Project Structure

```text
src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs
tests/BrownPants.Tests/NHibernateAwarenessTests.cs
README.md
specs/041-nhibernate-awareness/
```

No dependency or project evaluation changes are planned.
