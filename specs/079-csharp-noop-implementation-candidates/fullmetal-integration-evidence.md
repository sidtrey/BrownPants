# FullMetalFile live scan evidence

Command: `dotnet run --no-build --project src/BrownPants.Cli -- inspect <FullMetalFile-root> --format json --limit 10000` (run 2026-11-13; the generated report was kept outside the repository because it contains the checkout's local root path).

Observed from the generated inventory:

- C# files scanned: 327; bytes read: 1,435,741; skipped files: 0.
- Placeholder candidates: 17; omitted candidates: 0.
- Candidate scope: 2 production and 15 test.
- `FullMetalFile.Infrastructure/Persistence/PostgreSQL/PostgreSqlUnitOfWork.cs` reports `void Dispose()` at line 64 as an empty-body candidate and `NotImplementedException CreateNotImplemented()` at line 68 as a direct exception candidate.
- `CreateNotImplemented` is a helper used by accessors for repositories that are not implemented yet. The report is a source navigation lead and does not independently establish a defect.

The audit repository's earlier empty read methods were repaired in FullMetalFile Feature 021 before this analyzer was first run on the live checkout. Generated fixtures cover the original empty-result shapes. This feature reports only method bodies that match its specified direct patterns; detecting callers that delegate to a helper belongs to the separate indirect-accessor extension.
