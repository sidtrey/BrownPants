# Research: MFC Message Path Evidence

## Decisions

- **Represent dispatch as source-linked candidate relations.** Microsoft documents message-map macros as associations between command/message identifiers and member handlers. A unique local source match is useful evidence, but macro expansion, build configuration, and C++ overload binding remain unverified.
- **Start with common command macros and explicit class-qualified names.** Support `ON_COMMAND`, `ON_COMMAND_EX`, `ON_COMMAND_RANGE`, `ON_COMMAND_RANGE_EX`, `ON_UPDATE_COMMAND_UI`, and qualified handler references; add only forms covered by fixtures. Extend to common control-notification macros if the parser can extract their handler argument without broad matching.
- **Link class-qualified calls only when a unique definition exists.** A `(class, method)` index across discovered native sources is a transparent lexical link; overloaded, missing, or unsupported targets remain unresolved/ambiguous.
- **Expose candidates through existing inventory and evidence outputs.** These are the established human-readable and SDD-engine-neutral entry points. Do not claim a managed semantic call edge or alter the C#/VB semantic impact graph.
- **Preserve source safety.** Reuse or factor the current comment/string/raw-string masking, preserving line numbers, and never invoke MSBuild or a native compiler.

## Evidence

- The Brownfield Bug Lab contains `ON_COMMAND(ID_ORDER_IMPORT_BATCH, &COrderMfcView::OnImportBatch)` at `examples/brownfield-bug-lab/legacy/Native/OrderMfcView.cpp:6`; its handler at line 9 calls `COrderManagedBridge::ImportSelectedOrders()` at line 11. The bridge method is defined at `examples/brownfield-bug-lab/legacy/Bridge/OrderManagedBridge.cpp:5`.
- Current architecture analysis reports MFC message-map, MFC class, and C++/CLI markers independently. It does not associate the map with a handler definition or trace a method call.
- Microsoft Learn: [TN006: Message Maps](https://learn.microsoft.com/en-us/cpp/mfc/tn006-message-maps?view=msvc-170) describes command handlers and macro forms. [Message Maps (MFC)](https://learn.microsoft.com/en-us/cpp/mfc/reference/message-maps-mfc?view=msvc-170) shows the class-qualified `&CMyDoc::OnMyCommand` form.

## Alternatives Considered

- **Call these relations resolved:** rejected because lexical declaration matching does not prove macro expansion, overload selection, active preprocessor branch, or runtime dispatch.
- **Add a full C++ compiler/AST dependency:** rejected for this source-only portable feature; it would complicate native toolchain setup and project evaluation.
- **Only add more independent regex signals:** rejected because isolated findings do not show the path from the UI command to handler and bridge call.
- **Modify the existing managed impact graph:** rejected because that graph represents Roslyn-bound C#/VB method edges. Native candidates must remain a separate evidence family.
