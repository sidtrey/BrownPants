# Implementation Plan: WinForms Cross-Thread UI Access Candidates

**Branch**: `040-winforms-thread-safety` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Add an offline `winforms analyze` command using the existing Roslyn C# and Visual Basic parsers. Recognize property assignments on explicit WinForms Control types and source-declared derived types, then classify only locally visible callback and marshaling syntax. Reports show locations and evidence limits; they never claim an assignment is safe or a confirmed runtime defect.

## Technical Context

**Language/Version**: C# / .NET 10; parse C# and Visual Basic source
**Primary Dependencies**: Existing Roslyn compiler packages; no new runtime dependencies
**Storage**: None; read-only repository analysis
**Testing**: xUnit temporary-repository fixtures, deterministic Markdown/JSON, packed-tool smoke
**Target Platform**: Windows, macOS, Linux; analyzes source without launching Windows Forms
**Project Type**: .NET CLI analyzer
**Performance Goals**: Linear scan over discovered C#/VB files with bounded finding and diagnostic samples
**Constraints**: No MSBuild evaluation, network, source execution, repository writes, or source-text excerpts; classify only locally observed syntax; no claim of runtime thread identity or complete caller paths
**Scale/Scope**: Existing repository scanner bounds; output sample limit 1–10000; recognized `Task.Run`, `TaskFactory.StartNew`, `ThreadPool.QueueUserWorkItem`, `Thread.Start`, `BackgroundWorker.DoWork`, `Control.Invoke`/`BeginInvoke`/`InvokeAsync`, and `SynchronizationContext.Post/Send` patterns

## Constitution Check

- **Evidence First**: Each candidate includes the syntax and type-name evidence used to recognize a Control target, plus separate thread-context and marshaling observations. Unknowns remain unknown.
- **Broad, Engine-Neutral Core**: The command reads any repository containing C#/VB source and does not depend on Spec Kit or an SDD provider.
- **Deterministic and Reviewable Output**: Stable paths/locations, bounded findings, Markdown and versioned JSON; output excludes matched source text.
- **Safe Inspection by Default**: The command parses source only; it never loads MSBuild, builds, executes code, accesses the network, or writes files.
- **Specified and Verified Changes**: Fixtures cover C#/VB, comments/strings, Control type recognition, background callbacks, marshal patterns, path safety, parse issues, and output parity.

Gate: PASS. Syntax-only evidence cannot prove runtime thread identity, field aliasing, callers, or that marshaling succeeds. The command must use candidate terminology and preserve these limits.

## Research Decisions

See [research.md](research.md). Microsoft documents that Windows Forms controls must be accessed on their creating thread; `Invoke` and `BeginInvoke` marshal work, and .NET 9 adds `InvokeAsync`. `InvokeRequired` alone can return false before a handle exists, so its presence is a weak marker, not proof of safety. V1 uses syntax recognition only and reports observations without suppressing candidates as safe.

## Architecture and Data Flow

1. Discover files with `RepositoryScanner.DiscoverFiles`, preserving existing ignored-directory and path behavior.
2. Parse C# with the existing language-version selection from project metadata and parse Visual Basic with Roslyn; read project files as data only and never evaluate them.
3. Build source-local type maps from WinForms imports/aliases, known built-in Control class names, and custom types whose base chain reaches `Control`, `Form`, or `UserControl`.
4. Find property assignment nodes whose receiver can be tied to a known/control-derived type within the source-local map. Record unsupported receiver cases as coverage limits only when needed; do not guess based on a property name alone.
5. Walk the assignment's syntax ancestors to classify known background callbacks and direct dispatcher-delegate containment. Separately record `InvokeRequired`/dispatcher markers in the nearest method; markers do not prove a safe guard.
6. Render stable Markdown or JSON with candidate count, locations, type evidence, callback context, marshal observations, and parse coverage. Limit output samples and state omitted counts.

## Project Structure

```text
src/BrownPants.Cli/Analysis/WinFormsUiThreadingAnalyzer.cs
src/BrownPants.Cli/Analysis/WinFormsUiThreadingModels.cs
src/BrownPants.Cli/Output/WinFormsUiThreadingReportRenderer.cs
src/BrownPants.Cli/Analysis/WinFormsCommand.cs
src/BrownPants.Cli/Program.cs
tests/BrownPants.Tests/WinFormsUiThreadingTests.cs
tests/PackedWinFormsSmoke.ps1
integrations/winforms/README.md
specs/040-winforms-thread-safety/
```

**Structure Decision**: Follow existing analyzer/renderer separation. Keep the analysis behind a separate CLI command so default `inspect` output and semantics do not change. Do not require Windows Desktop targeting packs.

## Complexity Tracking

No constitution violation. Static recognition deliberately stops at source-visible type and syntax evidence; broad call-graph execution-context claims are not part of V1.
