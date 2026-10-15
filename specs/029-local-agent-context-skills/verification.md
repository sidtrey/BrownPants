# Verification: Local Agent Context Skills

## Local checks

- `pwsh -NoProfile -File tests/AgentSkillIntegrationTests.ps1` — passed. Covers content contract, Codex/Claude/both install destinations, byte-identical copy, idempotence, conflict preservation, both-host preflight, invalid roots/selector, escaping-link refusal, README links, and CI registration.
- `pwsh -NoProfile -File tests/CopilotPromptIntegrationTests.ps1` — passed.
- `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1` — passed, including live Spec Kit CLI setup and extension checks.
- `pwsh -NoProfile -File tests/OpenSpecInstallerTests.ps1` — passed.
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build` — passed, 101 tests.
- `pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/029-local-agent-context-skills/spec.md -TasksPath specs/029-local-agent-context-skills/tasks.md -EvidencePath specs/029-local-agent-context-skills/acceptance-evidence.json` — passed; 8 tasks and 12 criteria structurally covered.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/029-local-agent-context-skills/spec.md` — passed; SHA-256 `53884b2121047af45d34ca5e55792cd84e64788311d80b62b8b239aea9dfbd31` unchanged.
- BrownPants self-inspection: `dotnet run --configuration Release --no-build --project src/BrownPants.Cli -- inspect . --format markdown --limit 10` — passed; recognized the .NET solution, 26 C# files, 22 PowerShell files, active feature 029, and evidence limits.

## Hosted checks

Cross-platform CI is pending for this commit. Task T008 remains incomplete until the Windows, Linux, and macOS jobs complete successfully. Local checks do not launch either agent desktop host; vendor support boundaries are documented in the integration guide.
