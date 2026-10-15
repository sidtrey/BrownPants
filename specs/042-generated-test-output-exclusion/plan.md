# Implementation Plan: Generated Test Output Exclusion

**Branch**: `042-generated-test-output-exclusion` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Add three conventional test-result folder names to the repository scanner's existing ignored-directory set. Keep filtering at child-directory traversal so an explicitly selected directory is always scannable. Preserve existing ignore behavior and test the file discovery API and user-facing inspect command.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing .NET APIs; no new packages
**Storage**: None
**Testing**: xUnit temporary directory fixtures
**Target Platform**: Windows, Linux, macOS; case-insensitive directory-name matching
**Project Type**: .NET CLI repository scanner
**Performance Goals**: No additional filesystem passes; use the existing directory-pruning set
**Constraints**: Read-only; do not delete, modify, or follow excluded directory contents during a parent scan
**Scope**: `TestResults`, `test-results`, and `testresults`; current `bin`, `obj`, `coverage`, `artifacts`, and other ignore names remain unchanged

## Constitution Check

- Evidence First: tests assert exact included and excluded relative paths.
- Safe by Default: scanner prunes ignored children and never writes to input.
- User Control: explicit path selection uses existing command behavior to inspect an otherwise ignored folder.
- Deterministic: existing path sorting remains unchanged.

Gate: PASS. The explicit-root behavior follows from the scanner enumerating the selected root's children; tests must lock this behavior down.

## Architecture

Modify `RepositoryScanner.IgnoredDirectories` and add targeted `DiscoverFiles` and `Program.Run inspect` tests. Do not add a new inclusion option or broaden matching to generic names such as `test`, `tests`, or `fixtures`.

## Project Structure

```text
src/BrownPants.Cli/Analysis/RepositoryScanner.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
specs/042-generated-test-output-exclusion/
```
