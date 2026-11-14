# Verification: Source Work Marker Evidence

## Behavior

`inspect` now reports standalone `TODO`, `FIXME`, `HACK`, and `XXX` tokens from supported source files. Findings include repository-relative path, one-based line, column, and marker kind; they do not copy source lines. Markdown and JSON include total and omitted counts, source files and bytes scanned, and files skipped by limits or access errors.

The analyzer reads at most 1 MiB per file and 32 MiB total, follows the repository's existing discovery exclusions, and leaves documentation outside the marker scope. Marker candidates can occur in comments or string literals and do not prove defects.

## Observed checks (2026-11-02)

- `dotnet build BrownPants.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln --no-restore`: 399 passed, 0 failed, 0 skipped.
- Focused work-marker tests: 2 passed, including marker boundaries, source-only scope, deterministic ordering, limits, and Markdown/JSON output.
- BrownPants inspection of FullMetalFile: scanned 288 source files and found 45 markers with no omissions or skipped files. It located `FullMetalFile.Infrastructure/DocumentImport/DocumentImportService.cs:26` (`TODO`).
- `git diff --check`: passed.

The reviewable live JSON is in ignored `TestResults/feature-076-fullmetal-markers.json`; build and test output are in `TestResults/feature-076-dotnet-build.log` and `TestResults/feature-076-dotnet-test.log`.

## Current FullMetalFile result (2026-11-13)

A fresh scan of the current FullMetalFile checkout covered 187 supported source files and reported one work marker, zero omitted markers, and zero skipped files: `backend/src/FullMetalFile.Infrastructure/Ingestion/EmailIngestionWorker.cs:59` (`TODO`). The JSON report is retained locally in ignored `TestResults/feature-076-current-fullmetal-markers.json`; only the relative path and line are recorded here.

The earlier 45-marker and zero-marker counts are historical snapshots from different states of the FullMetalFile checkout. The current scan demonstrates that SC-003's existing path-and-line requirement is still met, so no acceptance-criteria or baseline change is needed.

On the merged feature branch, `dotnet build BrownPants.sln --no-restore` passed with 0 warnings and 0 errors, and `dotnet test BrownPants.sln --no-restore` passed 428/428.
