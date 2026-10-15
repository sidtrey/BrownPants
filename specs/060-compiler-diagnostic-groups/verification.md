# Verification: Compiler Diagnostic Groups

## Automated checks

- Test-first regression: the new aggregation test failed to compile before the group model existed; after implementation, both focused grouping/report tests passed.
- `dotnet test BrownPants.sln -c Release --filter "FullyQualifiedName~CompilerDiagnosticGroups|FullyQualifiedName~SemanticAnalyzer_GroupsCompilerErrorsByProjectAndCodeIndependentOfSamplingAndInputOrder"` — passed, 2 tests.
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 279 tests.
- Published CLI smoke — `dotnet publish src/BrownPants.Cli/BrownPants.Cli.csproj -c Release --no-restore -o <temporary-directory>`, then ran the published assembly's semantic JSON inspect; schema version 1, all 21 errors, and all four expected groups were present.
- Acceptance criteria hash — passed: `d5632ba633a39495940769093ea8d2995a086e46a2dd1b6afe4d2ca2d19604a1`.
- Acceptance evidence ledger — passed: 4 tasks mapped to 8 criteria.

## BrownPants-on-BrownPants

JSON command: `dotnet run --project src/BrownPants.Cli -- inspect . --semantic --format json --limit 10000`.

The report kept schema version 1 and the existing 21 compiler errors and 21 raw samples, with zero omitted errors or groups. It returned four deterministically sorted project/code groups:

| Project | Code | Count |
|---|---|---:|
| `examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj` | CS0246 | 3 |
| `examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj` | CS0518 | 12 |
| `examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj` | CS0549 | 4 |
| `examples/brownfield-bug-lab/legacy/Framework/LegacyOrderDesk.csproj` | CS1729 | 2 |

Markdown at `--limit 10` showed `4 total; 0 group samples omitted` before the existing raw samples, which correctly reported 11 additional error rows omitted. A fixture also confirmed at `--limit 1` that 2 of 3 groups and 2 of 3 raw rows are omitted independently. These code frequencies do not diagnose why the errors occurred or claim a fix.
