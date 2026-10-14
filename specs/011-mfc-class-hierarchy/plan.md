# Implementation Plan: Report MFC Class Inheritance Signals

**Branch**: `011-mfc-class-hierarchy` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/011-mfc-class-hierarchy/spec.md`

## Summary

Extend the existing native C++ candidate analyzer to identify class and struct declarations that inherit from common MFC framework types. Preserve comment/string filtering and report the declaration start, sanitized evidence, and candidate evidence level in both report formats.

## Technical Context

**Language/Version**: C# / .NET 10; analyzed source is native C++
**Primary Dependencies**: Existing .NET regular expressions and source scanner
**Storage**: Read-only C++ source files in the scanned repository
**Testing**: xUnit temporary repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: Bounded declaration buffering and one lexical pass over each native source file
**Constraints**: No C++ compiler, MSBuild evaluation, external parser, network, or writes during inspection
**Scale/Scope**: Common MFC application and UI base classes; no complete C++ grammar or semantic resolution

## Constitution Check

- **Evidence First**: Results remain explicitly labeled lexical candidates with source evidence. PASS.
- **Broad, Engine-Neutral Core**: Improves native context for any SDD workflow without adding an engine dependency. PASS.
- **Deterministic and Reviewable Output**: Existing ordering, sample limits, path and line fields apply. PASS.
- **Safe Inspection by Default**: Reuse existing source reads and comment/string filtering; do not execute builds or tools. PASS.
- **Specified and Verified Changes**: Fixture coverage includes inheritance, multiline declarations, comments, strings, and unrelated base classes. PASS.

## Design

Keep existing native macro patterns unchanged. Track a bounded class/struct declaration after its keyword, append sanitized lines until a body opener or forward-declaration terminator, and check for one of the documented common MFC base names. Emit one `MFC class inheritance` signal at the declaration's first line. Bound the tracked declaration by line count and character count, and discard it if incomplete or too large. Use sanitized source as evidence so comments and string literals are not surfaced as matches. Existing report serialization already exposes architecture candidate signals, so no model or JSON schema change is needed.

### Recognized Base Types

Start with common framework types: `CObject`, `CCmdTarget`, `CWinThread`, `CWinApp`, `CWinAppEx`, `CWnd`, `CFrameWnd`, `CFrameWndEx`, `CMDIFrameWnd`, `CMDIFrameWndEx`, `CMDIChildWnd`, `CView`, `CScrollView`, `CFormView`, `CListView`, `CTreeView`, `CDocument`, `CDialog`, `CDialogEx`, `CPropertyPage`, `CPropertySheet`, `CControlBar`, `CToolBar`, `CStatusBar`, `CRecordView`, and `CRecordset`.

### Expected Repository Changes

```text
src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
README.md
```

## Validation

Run `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, the Spec Kit and OpenSpec adapter checks, and inspect Markdown/JSON candidate output.

## Complexity Tracking

No constitution violations or new dependencies.
