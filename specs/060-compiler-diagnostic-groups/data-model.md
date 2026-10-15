# Data Model: Compiler Diagnostic Groups

## Compiler diagnostic group

- `Project`: normalized repository-relative project path.
- `Code`: Roslyn compiler diagnostic identifier, such as `CS0518`.
- `Count`: exact number of error diagnostics with this project/code pair across analyzed compilations.

Groups are sorted by `Project`, then `Code`, using ordinal ordering. The semantic report includes sampled `CompilerDiagnosticGroups`, exact `CompilerDiagnosticGroupCount`, and `CompilerDiagnosticGroupOmittedCount`. Group samples are limited independently from raw diagnostic samples by the same user-selected limit. `CompilerErrorCount`, `CompilerErrorOmittedCount`, and `CompilerDiagnostics` keep their existing meanings and values.

The count is descriptive evidence. A group does not claim that diagnostics share a cause, that the project is invalid, or that changing a reference or source line will fix them.
