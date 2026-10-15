# Quickstart: Native Call Impact

From the repository root, build and test:

```powershell
dotnet build BrownPants.sln -c Release
dotnet test BrownPants.sln -c Release --no-build
```

Run impact against the Brownfield Bug Lab's MFC view and bridge:

```powershell
dotnet run --project src/BrownPants.Cli -- impact examples/brownfield-bug-lab --changed legacy/Native/OrderMfcView.cpp --changed legacy/Bridge/OrderManagedBridge.cpp --format markdown
```

Expected evidence includes the MFC `ID_ORDER_IMPORT_BATCH` map entry, `COrderMfcView::OnImportBatch`, and the qualified call to `COrderManagedBridge::ImportSelectedOrders`. Native findings must be labeled candidate-level and include source locations and limits. Repeat with `--format json` to inspect machine-readable fields. The fixture must remain unchanged by inspection.
