# Verification: Transitive Managed-to-Native Impact

## Regression and focused tests

- Added `Analyze_TracesManagedCallerDeclarationToNativeAndMfcCandidates` first. Before the implementation, the test build failed because `NativeCallImpact.ManagedImpactDepth` did not exist. After implementation, the focused test passed. It verifies exact path/line/method matching, same-file and same-location symbol decoys, shortest managed-depth selection, separate depth dimensions, and visible ambiguous/unresolved rows.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter FullyQualifiedName~Analyze_TracesManagedCallerDeclarationToNativeAndMfcCandidates`: 1 passed.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter FullyQualifiedName~CallImpactAnalyzerTests`: 15 passed.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --filter FullyQualifiedName~Evidence_FocusByChangedRepositoryIncludesOnlyNativeTraceForImpactedManagedCaller`: 1 passed.

## Brownfield Bug Lab evidence

Command:

```powershell
dotnet run --project src/BrownPants.Cli -c Release --no-build -- impact examples/brownfield-bug-lab --changed managed/Persistence/OrderRepository.cs --format json --limit 1000
```

Observed complete report: 9 managed impacts and 3 native candidates. Each native row has `ManagedImpactDepth: 1`; `Depth` remains the native-only distance:

| Evidence | Status | Managed depth | Native depth | Location |
|---|---|---:|---:|---|
| C++/CLI managed call | candidate | 1 | 1 | `legacy/Bridge/OrderManagedBridge.cpp:10` |
| Qualified native call | candidate | 1 | 2 | `legacy/Native/OrderMfcView.cpp:11` |
| MFC dispatch | candidate | 1 | 3 | `legacy/Native/OrderMfcView.cpp:6` |

Direct impact from `managed/Workflows/OrderImportWorkflow.cs` remains complete with three native candidates at native depths 1, 2, and 3.

`evidence --semantic --changed managed/Persistence/OrderRepository.cs` reports the same three native finding identities and locations, marks impact complete, and emits unique finding IDs. A fixture also verified that an unrelated `Workflow.Ignore` method in the same managed file is excluded.

## Release gates

- `dotnet build BrownPants.sln -c Release`: succeeded, 0 warnings, 0 errors.
- `dotnet test BrownPants.sln -c Release`: 285 passed, 0 failed, 0 skipped.
- `git diff --check`: passed; Git reported only its configured LF-to-CRLF conversion notices for edited files.
- Acceptance criteria SHA-256: `93cf07920744e900f7e7d93fe8792a967e9e2e247081e195e3d8c1c9cbbc8954`.

## Evidence limits

The native bridge, qualified call, and MFC dispatch remain lexical candidates. The trace does not prove compiler binding, runtime execution, or exhaustive impact. Managed-derived relation joins require exact repository path and line plus normalized method identity; unresolved name-only relations are retained as unresolved evidence and reduce completeness.
