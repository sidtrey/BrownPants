# Verification: Grouped Candidate Evidence

## Acceptance evidence

- **AC-001 / SC-001** — BrownPants-on-BrownPants bug-lab evidence emitted 27 findings, five repeated candidate groups, and no truncation. The checked-in group snapshot lists exact lines and existing IDs for C++/CLI bridge `[5, 10, 11]`, MFC message map `[5, 7]`, NHibernate session factory `[7, 9]`, NHibernate session `[13, 15]`, and NHibernate transaction `[14, 16]` in `examples/brownfield-bug-lab/evaluation/grouped-candidate-evidence-v4.json`.
- **AC-002 / SC-002** — `RepeatedCandidateGroups_KeepDifferentCategoriesSignalsAndPathsSeparate` verifies exact category/signal/path keys, stable category/path/signal ordering, and candidate-only membership. The CLI fixture also proves a matching signal in a second path stays separate.
- **AC-003** — The full-report fixture keeps schema version 1 and asserts raw findings and completeness counts remain the emitted source rows. Each group location links the corresponding unchanged finding ID and line.
- **AC-004** — `Evidence_FocusedRepeatedCandidateGroupsContainOnlySelectedFindingIdsAndKeepReasons` verifies clue plus changed-path filtering leaves only selected IDs in the group and keeps both `matchedBy` reasons on the canonical findings.
- **AC-005 / SC-003** — The v4 scorer self-test matched all 20 expected facts, rejected both decoys, accepted an exact `lines` array, and rejected an extra unrelated line. Final matched conditions each scored 20/20 (100% recall, 86.96% precision); both used the same three false positives and made an identical fix. The assisted precision exceeds the earlier recorded 74.1%, but it did not exceed baseline. The pilot and prompt revision are recorded to avoid presenting the final prompt as an untouched trial.
- **SC-004** — Grouping runs only over emitted findings; focused reports rebuild from their emitted subset. The bug-lab and repository self-evidence runs both reported `findingsTruncated: false`.

## Commands and observed results

- `dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~RepeatedCandidateGroup` — passed, 3 tests.
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 282 tests.
- `dotnet run --project src/BrownPants.Cli -- evidence examples/brownfield-bug-lab --format json --limit 10000` — 27 findings, five groups, complete.
- `dotnet run --project src/BrownPants.Cli -- evidence . --format json --limit 10000` — 64 findings, six groups, complete.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/061-grouped-candidate-evidence/spec.md` — verified `6de39c8f5bc21c7b4a88ac49e9b2202c07cab57329571a8bee7d917ed6bcd2c4`.
- `pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/061-grouped-candidate-evidence/spec.md -TasksPath specs/061-grouped-candidate-evidence/tasks.md -EvidencePath specs/061-grouped-candidate-evidence/acceptance-evidence.json` — passed; 5 tasks and 9 criteria structurally verified.

The frozen bug-task acceptance hash remains `777ecb0e7523c67dc4a3e8b28cebae70e7b9476939c00a65eea50308c7d3dd63`. Benchmark results, full agent responses, source and prompt hashes, token use, and limitations are in `docs/comparisons/brownfield-evidence-grouping-2026-10-results.json` and the adjacent report.
