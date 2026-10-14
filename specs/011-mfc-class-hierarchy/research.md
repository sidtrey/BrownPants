# Research: MFC Class Inheritance Signals

## Evidence

- Microsoft Learn's [MFC class hierarchy chart](https://learn.microsoft.com/en-us/cpp/mfc/hierarchy-chart?view=msvc-170) documents MFC inheritance rooted in framework types such as `CObject`, `CCmdTarget`, and `CWnd`.
- The [CFrameWnd reference](https://learn.microsoft.com/en-us/cpp/mfc/reference/cframewnd-class?view=msvc-170) shows the declaration `class CFrameWnd : public CWnd` and explains its role in document/view applications.
- Current BrownPants native analysis already strips comments and strings and reports MFC runtime macros, message maps, object factories, COM, C++/CLI, and smart-pointer candidates. It does not inspect base classes in class or struct declarations.

## Decisions

- Add a separate `MFC class inheritance` signal category for recognized MFC base class names in native class declarations.
- Collect bounded declaration text across lines after removing comments and strings. Match only the declaration prefix before its body or forward-declaration terminator.
- Report the declaration's first line and sanitized declaration evidence. Keep `EvidenceKind: Candidate` and `Language: C++`.
- Use a finite list of common application, window, view, document, and dialog base types. Do not claim full hierarchy resolution, macro expansion, compilation, or runtime use.

## Alternatives Considered

- Match any occurrence of a `C...` type name: rejected because it would label ordinary method parameters and variables as inheritance.
- Add a full C++ parser: rejected for this feature because it adds platform/toolchain requirements and changes the analyzer's evidence level.
- Match only one physical line: rejected because common class declarations can split the base list across lines.
