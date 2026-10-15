# Verification: Unique Semantic Evidence

## Regression

- Test-first check: `dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~Evidence_EmitsUniqueSameLineConstructorFindings` failed before the implementation because only one of two same-line constructor findings was emitted (expected 2, actual 1).
- After implementation, the focused regression passed. It exercises duplicate constructor and method calls on one line; verifies distinct IDs and source columns, deterministic full output, stable ID for a previously unique finding, exact ID retention in `--changed` focus, and accurate completeness.

## Brownfield Bug Lab evidence

- Full: `dotnet run --project src/BrownPants.Cli -c Release --no-build -- evidence examples/brownfield-bug-lab --semantic --format json --limit 1000` — 38 managed-call relationships emitted; 66 total and emitted findings; 66 unique IDs; all 38 managed findings have one-based columns; `findingsTruncated=false`; semantic coverage complete.
- Focused: same command with `--changed managed/tests/Program.cs` — 17 managed-call relationships selected, all IDs unique, semantic coverage complete, and no truncation.
- Two previously lost constructor rows now appear at `managed/tests/Program.cs:15` (`new Order(3, false)`) and `managed/tests/Program.cs:22` (`new ImportedRow(5, false)`).
- JSON schema remains version 1; source column is additive and omitted from non-semantic candidate rows.

## Repository checks

- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 283 tests.
- Acceptance criteria SHA-256: `54ae382bc70a122824bbe7e7f67c1238f99231db2d5089db4c0bd935a8c3ed88`.
- Acceptance evidence ledger: 5 tasks map to all 7 criteria.
