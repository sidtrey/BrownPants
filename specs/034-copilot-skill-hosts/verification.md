# Verification: BrownPants GUI Skill for GitHub Copilot

## Acceptance criteria

- Acceptance criteria SHA-256 verified as `f9d193ac2ed1c3588bb7c7d08a559c5d75ac3397326ed723a29cb1eb46ebf291`.
- Evidence ledger validation passed with 4 tasks and 9 criteria mapped.
- T001–T003 passed; T004 records this verification.
- AC-001/AC-002: Windows and Linux PowerShell checks installed byte-identical skill copies into Copilot-only and all-host targets; existing Codex, Claude, and `both` behavior passed.
- AC-003: all-host conflict preflight preserved the conflicting Copilot file and wrote neither other host destination.
- AC-004/AC-005: skill and root integration documentation describe restricted-terminal fallback, host-specific prompt/skill support, activation limits, and Visual Studio version requirements; documentation assertions passed.

## Commands

- `pwsh -NoProfile -File tests/AgentSkillIntegrationTests.ps1` — passed on Windows PowerShell 7.
- `docker run --rm --mount type=bind,source=F:/PythonLab/brownpants,target=/repo -w /repo mcr.microsoft.com/powershell:lts-alpine pwsh -NoProfile -File tests/AgentSkillIntegrationTests.ps1` — passed in Linux PowerShell 7. Image digest: `sha256:d00c2db9004b4c97c5ee8a512f7a8da295c6bfd79f63966d4a0771c929896d`.
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 163 tests.
- `pwsh -NoProfile -File tests/CopilotPromptIntegrationTests.ps1` — passed.
- `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1` — passed, including live Spec Kit CLI setup and extension checks.
- `pwsh -NoProfile -File tests/OpenSpecInstallerTests.ps1` — passed.
- Acceptance hash and evidence-ledger validators — passed.

## Limits

Installer checks validate files and paths, not Copilot UI launch behavior. Host support depends on vendor versions and organizational settings; agent-skill discovery does not guarantee activation. The integration does not bypass terminal restrictions.
