# Verification: Copilot GUI Context Workflow

**Date**: 2026-10-14

## Local checks

- `pwsh -NoProfile -File tests/CopilotPromptIntegrationTests.ps1` — passed on Windows PowerShell 7; prompt guidance, install, idempotent reinstall, conflicting-file preservation, invalid roots, and outside-project junction refusal passed.
- `dotnet build BrownPants.sln --configuration Release` — passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln --configuration Release --no-build` — passed, 90 tests.
- `tests/AcceptanceCriteriaHashTests.ps1` — passed.
- `tests/AcceptanceCriteriaIntegrityWorkflowTests.ps1` — passed.
- `tests/AcceptanceCriteriaReviewTests.ps1` — passed.
- `tests/AcceptanceEvidenceTests.ps1` — passed.
- `tests/SpeckitExtensionTests.ps1` — passed, including the live Spec Kit CLI setup and extension check.
- `tests/OpenSpecInstallerTests.ps1` — passed.
- Feature 026 acceptance digest verified: `3d64c1dc901c66ebea370bd2260d10e90e17f969c771ce95650e21206c9a0fff`.
- Feature 025 remains closed as Won't do; its original digest still verifies as `f1568f6b3b6f75e84aeaacef62649c6d0dfef985798bd5fed1c17ee056341556`.

## Hosted cross-platform verification

- GitHub Actions run [38665619392](https://github.com/sidtrey/BrownPants/actions/runs/38665619392) for commit `43c8cd3abfdfd097043ef2afb7c42dcf67fa08b9` — passed on `ubuntu-latest`, `macos-latest`, and `windows-latest`.
- Each hosted job passed restore, Release build, xUnit suite, acceptance workflow/hash and evidence checks, Spec Kit and OpenSpec adapter checks, Copilot prompt integration checks, and packed-tool smoke test.
- The amendment-policy check was skipped because this change did not amend acceptance criteria.
- The automated tests do not launch Visual Studio or VS Code; host support is documented from official vendor references.
