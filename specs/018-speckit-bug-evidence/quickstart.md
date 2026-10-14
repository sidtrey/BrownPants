# Quickstart: Spec Kit Bug Assessment Evidence

## Prerequisites

- Spec Kit CLI 1.2.0 or newer with Codex skills enabled, the official bug extension, and BrownPants installed.
- BrownPants on `PATH` or in the repository's `.tools` directory.

## Validate

1. In a disposable Spec Kit project, run `specify extension add bug`.
2. Install BrownPants from `integrations/speckit` with `specify extension add <path> --dev`.
3. Confirm `.specify/extensions.yml` contains enabled optional `before_bug_assess` for `speckit.brownpants.bug-evidence`.
4. Run `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1`.
5. Assess a pasted report with a distinctive symbol/error and a unique slug.
6. Confirm `.specify/bugs/<slug>/brownpants-evidence.md` has bounded paths/lines, no source snippets, and evidence limits.
7. Repeat with no safe clues or no CLI and confirm Spec Kit assessment continues with a skip notice.

Spec Kit remains responsible for assessment, fix, and test. Verify referenced files directly before making behavioral or causal claims.

## Validation performed

- `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1` — passed with live Codex setup, optional hook registration, and installed bug-assessment dispatch.
- Temporary bug-evidence fixture smoke — passed; saved report contained source/test locations and omitted matched source-line content.
- `dotnet build BrownPants.sln -c Release` — passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 56 tests.
- `pwsh -NoProfile -File tests/OpenSpecInstallerTests.ps1` — passed.
- Packed CLI smoke (`--help` and JSON `investigate`) — passed.
- `git diff --check` — run after final edits.
