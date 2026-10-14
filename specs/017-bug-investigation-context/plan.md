# Implementation Plan: Bug Investigation Context

**Branch**: `017-bug-investigation-context` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/017-bug-investigation-context/spec.md`

## Summary

Add a read-only `investigate` CLI command that accepts repeatable literal clues, searches repository file paths and contents using the existing deterministic discovery rules, and reports clue-linked source and test candidates in Markdown or JSON. It must not infer cause, execute project code, or change the existing `inspect` JSON contract.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing .NET libraries; no new packages
**Storage**: Read-only repository files
**Testing**: xUnit temporary-repository fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform CLI
**Performance Goals**: One bounded read pass over discovered files, deterministic output
**Constraints**: No builds, source execution, tests, or network calls; use the scanner's ignored-directory and symlink policy; do not include matched source contents in reports; skip binary and files over 10 MiB for content reads
**Scale/Scope**: Exact, case-insensitive clue matching; at most 20 clues of 1000 characters each; bounded samples and unchanged inspect contract

## Constitution Check

- Evidence First: report literal path/content match kinds and lines; clearly state matches do not establish cause. PASS.
- Broad, Engine-Neutral Core: provide CLI Markdown/JSON usable by Spec Kit, OpenSpec, or other SDD engines without adapter coupling. PASS.
- Deterministic and Reviewable Output: stable paths, clue order, match order, schema version, and explicit sample caps. PASS.
- Safe Inspection by Default: read only; no project evaluation, command execution, or network use. PASS.
- Specified and Verified Changes: fixture tests cover clues, noise, errors, output parity, and deterministic bounds. PASS.

## Design Decisions

- Add a distinct `investigate` command and report model; leave the `inspect` report schema unchanged.
- Accept repeated `--clue <text>` options so matching is explicit and does not require unreliable extraction of identifiers from natural-language bug reports.
- Match each clue literally, case-insensitively, against repository-relative paths and file lines. Report path and content matches separately; use repository-relative path, one-based line for content matches, and test-path classification.
- Share repository file discovery with `RepositoryScanner` so ignored directories, ordering, and symlink rules cannot drift between commands.
- Reuse a validated `--format markdown|json` and bounded `--limit 1..10000`; preserve total match counts while limiting samples.
- Limit investigations to 20 clues of at most 1000 characters each; skip binary files and files larger than 10 MiB for content matching while retaining path matches and bounded diagnostic codes.
- Do not output matched line contents, avoiding accidental exposure of source secrets in copied SDD context.
- Read failures for individual files produce diagnostics and do not discard other matches. Invalid root/CLI arguments return actionable errors.

## Research

See [research.md](research.md). This feature uses only existing project conventions and platform APIs; there are no external framework or API choices to research.

## Project Structure

```text
src/BrownPants.Cli/Program.cs
src/BrownPants.Cli/Analysis/RepositoryScanner.cs
src/BrownPants.Cli/Analysis/RepositoryInvestigator.cs
src/BrownPants.Cli/Analysis/RepositoryInventory.cs
src/BrownPants.Cli/Output/InvestigationReport.cs
tests/BrownPants.Tests/RepositoryScannerTests.cs
README.md
specs/017-bug-investigation-context/
```

## Validation

Run focused investigation fixture tests, Release build and full suite, Spec Kit/OpenSpec adapter checks, `git diff --check`, and repeat both Markdown and JSON commands on an unchanged fixture to verify stable output and parity.
