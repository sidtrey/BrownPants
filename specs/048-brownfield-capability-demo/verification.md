# Verification: Brownfield Capability Demo

## Fixture and BrownPants evidence

- The managed .NET 10 console checks are package-free. The seeded precheck failed in both isolated conditions with `Batch import saved 1 of 2 orders.` The frozen source hash was identical in both copies: `824F0DF68674A89D352DB4D9DA764DDF8552C73803600AC8207A8273B2453F47`.
- Repository CLI inspection completed against the fixture and reported 21 source-linked candidate signals, including factory/DI, MFC/C++/CLI, and NHibernate dependency/session/configuration/mapping artifacts. `Noise.cpp` was not reported as an MFC signal.
- Semantic impact reported four managed callers and one transitive path. It resolved the single-item and batch `Save` overloads, both workflow calls, and `SaveMapped<TSource>` forwarding to `Save(IEnumerable<T>)`. The `AuditJournal.Save` decoy was not reported as an `OrderRepository<T>` caller.
- MFC/COM/C++/CLI and NHibernate evidence is labeled as candidate evidence; neither native execution nor ORM runtime/database behavior is established.

## Paired run

- The completed pair used Codex CLI 0.162.1 with `gpt-6-luna` on Windows, PowerShell 7.6.6, and .NET SDK 10.0.402. Both conditions used the same prompt SHA-256 `BAE85DC4E442CE8C9575D5B1D27C8D1D1CE4F7AA2FE78238F4C70058A6F25E59` and the task acceptance hash `777ecb0e7523c67dc4a3e8b28cebae70e7b9476939c00a65eea50308c7d3dd63`.
- Both agents completed. Each changed only `managed/Persistence/OrderRepository.cs` in its isolated source tree. Their final file hashes are identical: `4C92563FA6E7F45D666F5CF899514201613EE4E00CC2FFFE91AC45B52398C350`.
- The driver independently verified the acceptance hash and ran `dotnet run --project managed/tests/LegacyOrderDesk.Checks.csproj -c Release` after each fix. Both passed. Each agent's own final message reported its local hash/test checks as unavailable; the independent driver results are authoritative here.
- The scorer self-test passed: all 20 expected findings matched, both decoys were rejected, and fully qualified/short generic signatures normalized consistently.
- Frozen scoring: Spec Kit submitted 7 findings with 5 true positives, 2 false positives, 15 false negatives (precision 71.4%, recall 25%). Spec Kit + BrownPants submitted 9 with 6 true positives, 3 false positives, 14 false negatives (precision 66.7%, recall 30%). Managed-call precision and recall were 100% in both outputs. Recall improved five percentage points overall, while precision fell; the 20-point/no-precision-loss advantage gate was not met and no superiority claim is made.
- Completed-turn usage: baseline 299,072 input tokens (269,824 cached) and 3,531 output; assisted 389,425 input (344,576 cached) and 3,331 output. The assisted run used more input tokens. No time or cost claim is made.
- Full machine-readable counts, per-domain results, host details, and hashes are in `docs/comparisons/brownfield-bug-lab-2026-10-results.json`. The public README and comparison report now label this as a measured case study with no overall advantage established.

## Reproduction commands

```powershell
dotnet run --project src/BrownPants.Cli -- inspect examples/brownfield-bug-lab --format markdown --limit 1000
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab/managed --changed Persistence/OrderRepository.cs --format markdown --limit 1000
dotnet run --project examples/brownfield-bug-lab/managed/tests/LegacyOrderDesk.Checks.csproj -c Release
pwsh -NoProfile -File examples/brownfield-bug-lab/evaluation/Score-Comparison.ps1 -AnswerKeyPath examples/brownfield-bug-lab/evaluation/answer-key.json -BaselineResultPath <baseline-result.json> -AssistedResultPath <assisted-result.json> -OutputPath <comparison.json> -SelfTest
pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/048-brownfield-capability-demo/spec.md
pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/048-brownfield-capability-demo/spec.md -TasksPath specs/048-brownfield-capability-demo/tasks.md -EvidencePath specs/048-brownfield-capability-demo/acceptance-evidence.json
git diff --check
```

`Prepare-Comparison.ps1` requires an output path that does not already exist and creates the matched seeded copies there. The prepared task files are hashed and not edited during an agent run.
