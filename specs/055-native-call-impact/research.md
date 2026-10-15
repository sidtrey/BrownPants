# Research: Native Call Impact

## Existing Evidence

- BrownPants self-inventory discovers C/C++ files and reports native flow as lexical candidates.
- `NativeFlowRelation` supplies kind, status, source/target paths and lines, source/target symbols, candidate count, and command identifier.
- The Brownfield Bug Lab currently yields two links: MFC `ID_ORDER_IMPORT_BATCH` to `COrderMfcView::OnImportBatch`, then a qualified call to `COrderManagedBridge::ImportSelectedOrders`.
- `CallImpactAnalyzer` and `EvidenceCommand.ValidateChangedPaths` currently accept only C# and VB changed paths.

## Decisions

- Reuse native-flow relations as the only source of native call edges. Do not add a C++ compiler or claim Roslyn-like binding.
- Traverse relations in reverse from changed native target locations, linking a qualified-call source symbol to an MFC-dispatch target symbol.
- Keep native and managed results visibly distinct, and report omitted/ambiguous/unresolved relation counts as completeness limits.
- Keep path validation repository-relative and based on discovered files.

## Alternatives Considered

- Parse C++ project files or invoke MSVC/Clang: rejected because it would add platform/toolchain requirements and violate the source-only native evidence boundary.
- Put candidate edges into the existing managed caller list without a type marker: rejected because it would falsely equate lexical candidates and compiler-resolved relationships.
- Return an empty complete report for unsupported native paths: rejected because it hides requested scope and creates false no-impact conclusions.
