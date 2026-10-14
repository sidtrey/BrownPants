# Data Model: Cross-Ecosystem Repository Inventory

## ProjectSystemInfo

- `Name`: Stable ecosystem/build-system label (for example `.NET`, `Python`, `Rust`, `CMake`, or `Containers`).
- `ManifestPaths`: Sorted, distinct repository-relative paths that triggered the classification.
- A system can have several manifests. A repository can report several systems.

## LanguageCoverageInfo

- `Language`: Stable language label derived from recognized source file extensions.
- `SourceFileCount`: Count of files for that language in the scanned repository.
- `AnalysisLevel`: One of `Roslyn syntax and candidate patterns`, `Lexical candidate patterns`, or `Inventory only`.
- C# semantic resolution remains represented by the existing optional semantic report and is not implied by this summary field.

## ExtensionCount

- `Extension`: Lowercase suffix, or `[no extension]` for extensionless files.
- `Count`: Number of discovered files with that suffix classification.

All records are in-memory scan output. JSON includes additive collections; Markdown renders the same stored evidence with stable ordering.
