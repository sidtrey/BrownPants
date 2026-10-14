# Implementation Plan: Spec Kit Bug Assessment Evidence

**Branch**: `018-speckit-bug-evidence` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: `specs/018-speckit-bug-evidence/spec.md`

## Summary

Register one optional BrownPants command on Spec Kit's `before_bug_assess` event. It uses the existing read-only `investigate` CLI to write bounded clue-linked Markdown evidence under the active bug directory, leaving the official assess/fix/test commands in control.

## Technical Context

**Language/Version**: Markdown and YAML; PowerShell 7 integration checks; .NET 10 CLI
**Primary Dependencies**: Spec Kit extension API and existing BrownPants `investigate` command; no new package
**Storage**: `.specify/bugs/<slug>/brownpants-evidence.md`
**Testing**: `tests/SpeckitExtensionTests.ps1`, including a disposable live Spec Kit project
**Target Platform**: Windows, Linux, macOS agent workspaces
**Project Type**: Cross-platform CLI and Spec Kit adapter
**Performance Goals**: One bounded local scan per assessment
**Constraints**: Optional hook; no URL retrieval, source execution, MSBuild evaluation, or source snippets; write only inside active bug directory; gracefully skip missing inputs/tools
**Scale/Scope**: One command, one hook, one report file, synchronized manifests, docs, and tests

## Constitution Check

- **Evidence First**: Preserve literal match provenance and limitations; no cause/coverage claims. PASS.
- **Broad, Engine-Neutral Core**: Keep lifecycle behavior inside the adapter; CLI remains reusable. PASS.
- **Deterministic and Reviewable Output**: Reuse existing ordering and limits. PASS.
- **Safe Inspection by Default**: Local read-only scan only; no network or project execution. PASS.
- **Specified and Verified Changes**: Test source/template/installed manifests and live optional hook registration. PASS.

## Design Decisions

- Register optional `before_bug_assess` as `speckit.brownpants.bug-evidence`; do not alter the existing context command.
- Reuse Spec Kit's resolved `BUG_SLUG` and `BUG_DIR`; never guess a path.
- Use up to 20 distinctive, non-sensitive literal clues from pasted report text in the active conversation. Do not fetch URLs.
- Invoke `brownpants investigate . --clue <text> --format markdown --limit 100`; use the repo-local `.tools` shim when needed.
- Save CLI output as `brownpants-evidence.md`; skip cleanly if prerequisites fail and never silently overwrite.
- Instruct assessment to verify referenced files and treat evidence as navigation leads.

## Project Structure

```text
integrations/speckit/extension.yml[.template]
integrations/speckit/commands/bug-evidence.md
integrations/speckit/README.md
integrations/README.md
.specify/extensions.yml
.specify/extensions/brownpants/extension.yml[.template]
.specify/extensions/brownpants/commands/bug-evidence.md
tests/SpeckitExtensionTests.ps1
specs/018-speckit-bug-evidence/
```

**Structure Decision**: Extend the existing adapter and its integration checks; no CLI source changes are needed.

## Validation

Run the Spec Kit integration test with the bundled bug extension installed, then the .NET build/test suite, OpenSpec adapter check, packed-tool smoke check, and `git diff --check`.

## Complexity Tracking

No constitution violations or new runtime dependencies.
