# Verification: Cursor BrownPants Context Command

Verified on 2026-10-15.

## Results

- `tests/CursorCommandIntegrationTests.ps1` passed on Windows PowerShell 7 and in the official PowerShell 7.5 Ubuntu Docker image. Both runs exercised copy, identical reinstall, conflict preservation, invalid roots, and an escaping directory link.
- Prompt-contract checks passed for the `/brownpants-context` command, bounded `brownpants inspect . --format markdown --limit 50`, source verification, candidate limits, and approved-shell/report fallback. Checks reject installer/network and semantic/MSBuild guidance.
- Copilot prompt, shared agent skills, OpenSpec installer, and Spec Kit extension integration scripts all passed on Windows.
- `dotnet build BrownPants.sln -c Release --no-incremental` passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build` passed: 174 tests, 0 failed.
- BrownPants-on-BrownPants inspection reported `specs/037-cursor-context-command` as the active work item.
- `scripts/acceptance-criteria-hash.ps1` verified digest `ddf525f57ac459fe1c208d85b76d658cc5b68baed65fe06eca33edeca747519a`.
- `scripts/check-acceptance-evidence.ps1` validates the task-to-criteria mappings and evidence files.

Cursor is not installed in this environment, so its signed-in GUI activation was not exercised. The adapter test proves the documented command file and safe installer contract, not runtime activation. Cursor documents project slash commands as a beta feature; users should confirm availability and terminal permissions in their organization's host configuration.
