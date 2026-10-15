# Verification: MFC Message Path Evidence

## Brownfield result

Ran `dotnet run --project src/BrownPants.Cli -c Release --no-build -- evidence examples/brownfield-bug-lab --format json --limit 1000` and inspected the matching Markdown inventory. The JSON report contained 24 total findings and two native-flow candidates, with no truncation:

1. `mfc-dispatch`, candidate: `legacy/Native/OrderMfcView.cpp:6`, command `ID_ORDER_IMPORT_BATCH`, links to `legacy/Native/OrderMfcView.cpp:9`, `COrderMfcView::OnImportBatch`.
2. `qualified-native-call`, candidate: `legacy/Native/OrderMfcView.cpp:11`, `COrderMfcView::OnImportBatch`, links to the inline `COrderManagedBridge::ImportSelectedOrders` definition in `legacy/Bridge/OrderManagedBridge.cpp:1`.

The report explicitly says macro expansion, active preprocessor branches, overload binding, unqualified calls, runtime dispatch, and C++/CLI-to-managed project linkage are unresolved. No native edge was added to the Roslyn managed-call graph.

BrownPants self-inspection covered 717 discovered files, found 35 architecture-pattern candidates, and rendered the same two native-flow relations from the checked-in MFC/C++CLI demo. The engine-neutral self-evidence report contained 59 findings with no truncation.

## Acceptance evidence

- **AC-001 / SC-001**: Temporary fixture and Brownfield Bug Lab verify MFC command-to-handler identity and both source locations.
- **AC-002 / SC-001**: Fixture verifies cross-file qualified-call matching and inline C++/CLI member definitions; the demo matches the handler call to the bridge definition.
- **AC-003 / SC-002**: Missing handler and overloaded handler/callee fixtures report unresolved or ambiguous status, null target locations, and exact candidate counts.
- **AC-004 / SC-002**: Line/block comments, ordinary strings, and raw strings containing fake maps/calls produce no relations.
- **AC-005 / SC-003**: Repeated reports retain relation identities/order; bounded scan reports omitted counts; before/after fixture file lists match. Packed smoke passes.
- **AC-005 / SC-003**: A C#-only repository omits the native-flow Markdown section, keeping generic repository reports focused.
- **SC-004**: Existing managed behavior remains green in the full solution suite; no managed impact code was changed.

## Checks run

- Red-first focused test initially failed to compile because the expected `NativeFlow` contract was absent; after implementation, the focused test passed.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-restore` — passed, 264 tests, 0 failed, 0 skipped.
- `dotnet pack src/BrownPants.Cli/BrownPants.Cli.csproj -c Release --no-restore -o <temporary-directory>` — passed; extracted CLI passed `tests/PackedNativeFlowSmoke.ps1`. `.github/workflows/ci.yml` runs the same packed smoke in the host matrix.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/054-mfc-message-path-evidence/spec.md` — verified `ec82e2cb49a0dad60922e54f8cd07d4b49e52ef3df13d145411e827d81318575`.
- Acceptance evidence ledger validator — passed for 7 tasks and 9 criteria.
- `git diff --check` — passed.

## Limits

Native parsing is lexical and intentionally bounded. Macro-generated code, active build configuration, overload selection, unqualified calls, runtime routing, and the managed target behind C++/CLI remain unverified. Candidate edges are navigation evidence, not proof of execution or causality.
