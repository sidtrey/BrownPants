# MFC Inheritance Signal Contract

Native source files may report the architecture category `MFC class inheritance` when a class or struct declaration names a recognized MFC base type.

- `EvidenceKind` is `Candidate`.
- `Language` is `C++`.
- `Path` and `Line` locate the start of the declaration.
- `Evidence` is sanitized, whitespace-normalized declaration text.
- Comments and string literals do not produce matches.
- Markdown and JSON expose the same bounded, deterministically ordered signals.

This signal does not prove that C++ parsed, that the type resolves to the framework class, or that the class participates in a built or running application.
