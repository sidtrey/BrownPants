# Verification: OpenSpec Review Workbench

Verified on 2026-10-15 with .NET SDK 10.

## Results

- `dotnet build BrownPants.sln -c Release --no-incremental` passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build` passed on Windows: 174 tests, 0 failed.
- `dotnet test BrownPants.sln -c Release` passed in the .NET 10 Linux Docker image: 174 tests, 0 failed.
- The nine `SddWorkbenchTests` passed as part of both full suites, covering Spec Kit defaults and OpenSpec valid/invalid engine selection, unique/explicit/ambiguous/archive/missing/outside selection, multiple delta files with repeated requirement titles, nested task states, source-path feedback IDs, hostile Markdown, input preservation, and offline HTML.
- Spec Kit extension and OpenSpec installer integration scripts both passed.
- `dotnet pack` created `BrownPants.Cli.0.1.0.nupkg`; the package installed from the signed local smoke configuration and its CLI generated an OpenSpec report from a fixture repository.
- Opened that packed report as a local file in Edge headless; proposal summary, source paths, requirements, and note controls rendered.
- Executed the report's note-export JavaScript in Node with browser API stubs; the version 1 JSON download contained path-qualified requirement references, and repeated titles had unique references.
- The fixture suite checks that unsafe Markdown is encoded, no external scripts/styles or fetch calls appear, and source artifacts remain unchanged.
- BrownPants-on-BrownPants `inspect` reports `specs/036-openspec-workbench` as active and completed without semantic project loading.
- `scripts/acceptance-criteria-hash.ps1` verified digest `687df8d9e55c5a2789ebdbbfe7feb704b40a902e64b45e2089830db14872ab3b`.
- `scripts/check-acceptance-evidence.ps1` validates the task-to-criteria mappings and evidence files.

No third-party package was added. The browser and JavaScript runtime checks used installed local tools; neither is required by the BrownPants package. The workbench displays recorded OpenSpec task state and does not validate, edit, or archive changes.
