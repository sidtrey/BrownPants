# Verification: Focused Evidence Handoff

## Acceptance evidence

- **AC-001 / SC-001**: Added and passed a no-focus fixture asserting the response omits `focus` and `matchedBy`; existing full report fields remain serialized as before.
- **AC-002 / SC-002**: Clue fixtures select source findings at matching lines and file-level test candidates at matching test files. A second clue with no repository match remains visible with `matched: false`.
- **AC-003 / SC-002**: Semantic changed-path fixtures include a direct caller, a transitive test caller, and the related test-path candidate; unrelated source files are excluded. A dot-prefixed source directory also resolves its caller correctly.
- **AC-004**: Changed-path mode without `--semantic` emits only local candidate findings and states caller impact was not analyzed.
- **AC-005 / SC-004**: Clue and changed-path selectors are unioned; finding IDs remain unique and a finding selected both ways records both reasons.
- **AC-006 / SC-003**: Empty clues, traversal paths, absolute paths, missing paths, and unsupported extensions return actionable input errors without reading outside the repository.
- **AC-007 / SC-004**: Focused limit tests reconcile unique matched findings with emitted plus omitted counts and set the truncation flag when appropriate.
- **SC-005**: The packed CLI was run on the managed Brownfield Bug Lab. The full semantic report had 40 known findings and emitted 38; the focused handoff emitted 23. A comparison against the constructor-impact answer key found all 10 expected related facts and no missing facts. The report states `sourceEvidenceTruncated: true` because the upstream semantic handoff omitted two findings; no completeness claim is made.

## Checks run

- `dotnet test BrownPants.sln --filter FullyQualifiedName~EvidenceCommandTests` — passed, 20 tests.
- `dotnet test BrownPants.sln` — passed, 258 tests.
- `dotnet build BrownPants.sln --no-restore` — passed, 0 warnings and 0 errors.
- `dotnet pack src/BrownPants.Cli/BrownPants.Cli.csproj -c Release --no-restore -o <temporary-directory>` — passed; extracted packed CLI help and focused evidence commands ran successfully.
- Frozen feature acceptance SHA-256 — verified as `1db3d5958568434a721706be98e5ff140d2f5142953a2a0a60e6e15da4688791`.
- Acceptance evidence ledger validator — passed for all seven tasks and all twelve criteria.
- `git diff --check` — passed.

The CLI uses the full 10,000 finding sample for focused selection, then applies the requested output limit. If evidence exceeds that upstream sample, `focus.sourceEvidenceTruncated` and the report limitations state that selection may be incomplete. Clue matching remains literal and changed-path call impact remains limited to compiler-resolved C#/VB edges.

## Follow-up check on a real repository

- Ran the Release source CLI against the clean local Discord.Net checkout (1,960 discovered files; 1,690 C# files) with `evidence --clue Gateway --format json --limit 5`. The report retained one focused finding and showed 548 literal clue matches.
- The checkout contains 72 binary image files skipped by clue reading. Focused evidence now reports the bounded diagnostic summary `BinaryFileSkipped=72` in its existing limitations field instead of only saying that files were skipped. The standalone `investigate` command was used to confirm the diagnostic code; source remains read-only and semantic/MSBuild analysis was not requested.
- Added `Evidence_FocusedClueReportsBoundedReadIssueCodes` in `tests/BrownPants.Tests/EvidenceCommandTests.cs`; the Release suite passed all 388 tests after the change.
