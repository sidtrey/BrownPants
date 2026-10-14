# Feature 021 Verification

Verification was run locally on Windows with PowerShell 7 and the .NET 10 SDK.

| Check | Command | Observed result |
|---|---|---|
| Release build | `dotnet build BrownPants.sln --configuration Release` | Passed; 0 warnings, 0 errors. |
| xUnit suite | `dotnet test BrownPants.sln --configuration Release --no-build` | Passed; 75 passed, 0 failed, 0 skipped. |
| Evidence checker fixtures | `pwsh -NoProfile -File tests/AcceptanceEvidenceTests.ps1` | Passed, including mapping, task state, schema, and path-safety cases. |
| Baseline review fixtures | `pwsh -NoProfile -File tests/AcceptanceCriteriaReviewTests.ps1` | Passed against temporary Git histories for new, modified, late-added, deleted, and renamed baselines. |
| Workflow assertions | `pwsh -NoProfile -File tests/AcceptanceCriteriaIntegrityWorkflowTests.ps1` | Passed; label events, base/head inputs, and checker wiring are present. |
| Spec Kit integration | `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1` | Passed with the Spec Kit CLI, including installed Codex guidance parity. |
| OpenSpec integration | `pwsh -NoProfile -File tests/OpenSpecInstallerTests.ps1` | Passed. |
| Acceptance hash fixtures | `pwsh -NoProfile -File tests/AcceptanceCriteriaHashTests.ps1` | Passed. |
| All recorded baselines | `scripts/acceptance-criteria-hash.ps1` for every `specs/**/acceptance-criteria.sha256` | Passed for all 3 baselines. Feature 021 digest: `e2222f43d44c50304d0f4a2b4dd71299065c0a728bb235212c0b7478f550bf28`. |
| Pull request baseline policy | `pwsh -NoProfile -File scripts/check-acceptance-criteria-review.ps1 -BaseRef origin/main -HeadRef HEAD` | Passed against the committed feature; its first baseline is accepted because the feature spec is new relative to the base. |
| Feature evidence ledger | `pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/021-acceptance-criteria-integrity/spec.md -TasksPath specs/021-acceptance-criteria-integrity/tasks.md -EvidencePath specs/021-acceptance-criteria-integrity/acceptance-evidence.json` | Passed; 9 tasks and 18 criteria mapped. |
| Packed tool smoke test | Pack to a temporary directory, install with `dotnet tool install`, then run `brownpants --help` and `brownpants inspect . --format json --limit 10` | Passed; schema version 1 and a nonempty inventory returned. |

These local results are from Windows. The CI matrix also runs Windows, Ubuntu, and macOS; those hosted results are produced by GitHub Actions after the change is pushed. Structural ledger checks do not establish that evidence semantically proves a criterion; review remains necessary.
