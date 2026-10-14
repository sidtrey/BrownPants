# Feature 023 Verification

## Acceptance criteria integrity

- Baseline: `8eb102de426ba5cce8a9e9d64209e018fa138a2166fb46fc9837563d0f8359f7`
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/023-call-impact-report/spec.md` — passed before and after implementation; digest unchanged.

## Build and tests

- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 81 passed, 0 failed, 0 skipped.
- Focused `CallImpactAnalyzerTests` — passed, 6 passed.

## BrownPants-on-BrownPants evidence

- `dotnet run --no-build --project src/BrownPants.Cli -- impact . --changed src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs --format json --limit 10000` — complete report: 9 direct caller methods, 71 transitive caller methods, 962 call-site findings, 7 affected files, 2 projects, 4 test-candidate files, maximum observed depth 6. JSON starts with `SchemaVersion: 1`.
- `dotnet run --no-build --project src/BrownPants.Cli -- inspect . --format json --limit 10000` — 277 files, 0 C# parse errors, 0 architecture candidates, active Spec Kit work item `specs/023-call-impact-report`.

The impact sample is static and limited to resolved C#/VB method invocations. Candidate tests are path-based leads, not proof of test coverage; constructor/object-creation, native C++/MFC, reflection, and dynamic dispatch edges are not represented. A maximum `--limit` was used for the self-impact run to avoid sample truncation.
