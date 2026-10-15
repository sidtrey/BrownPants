# Data Model: Scope-Aware WinForms Control Evidence

## Source File Context

- `path`: repository-relative source path.
- `language`: C# or Visual Basic.
- `imports` / `aliases`: file or namespace-scoped namespace and alias evidence.
- `types`: namespace-qualified type declarations in the file, including partial parts.
- `diagnostics`: bounded source parse errors.

## Indexed Source Type

- `identity`: namespace-qualified type name; used for inheritance and member lookup.
- `baseTypes`: declared syntax base types normalized using that file's imports/aliases.
- `isControl`: whether the type or a local base chain positively reaches a known WinForms Control.
- `members`: declared field/property identifiers, declared types, and the source scope that contributes imports/aliases.
- `partialParts`: source contexts contributing declarations to the same identity.

## Assignment Finding

Existing fields remain: path, line/column, receiver, property, type evidence, execution context, background callback, marshaling observation, and Candidate classification. Candidate eligibility requires a resolvable receiver type in the indexed source model.

## Coverage

Add total unresolved plausible assignment targets and bounded issue samples. Counts describe only parsed assignment syntax; they do not measure every possible dynamic/reflection access or runtime UI operation.
