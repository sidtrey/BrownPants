# Research: Valid Legacy Semantic Fixture

## Findings

- `LegacyOrderDesk.csproj` targets .NET Framework v4.7.2 and includes `OrderDataAccess.cs`, but does not import `Microsoft.CSharp.targets`.
- `packages.config` pinned NHibernate 5.5.2 for net472, but the project contained no assembly reference, so MSBuildWorkspace could not bind NHibernate symbols. A temporary old-style-project probe confirmed an exact PackageReference plus explicit HintPath to the standard NuGet cache restores and builds with the current SDK. The `win` runtime identifier reflects .NET Framework's Windows-only runtime and removes the workspace restore warning.
- BrownPants semantic inspection loads five projects and resolves 10,354 invocations, but emits one workspace issue and 21 compiler errors for this fixture. Its four unresolved calls are `ISessionFactory.OpenSession`, `ISession.BeginTransaction`, `ISession.SaveOrUpdate`, and `ITransaction.Commit`.
- A repository test demonstrates semantic analysis of a classic Framework project when it imports C# targets. This supports correcting the fixture metadata before treating the sample's results as analyzer shortcomings.
- The NHibernate 5.5.2 package page lists support for .NET Framework 4.6.1 and later. Current package metadata shows newer releases, but no dependency update is required by this feature; preserve the existing scenario pin. [NuGet package page](https://www.nuget.org/packages/NHibernate/5.5.2)

## Decision

Keep the classic project format and .NET Framework 4.7.2 target. Migrate the existing pin to NHibernate 5.5.2 PackageReference, point the classic project at its restored assembly in NuGet's package cache, declare C# 10 for the syntax already used, import C# targets, and make the proxy entity non-sealed. Do not add stubs or upgrade the dependency. Unsupported hosts must retain honest incomplete coverage.
