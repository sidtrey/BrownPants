# Data Model: GitHub Discovery Topics

A repository topic is a public classification label attached to the BrownPants GitHub repository.

| Field | Rule |
| --- | --- |
| Name | Required, lowercase; letters, numbers, or hyphens; maximum 50 characters |
| Purpose | One evidenced product purpose, workflow, language, or ecosystem |
| Repository set | At most 20 unique topic names |

The reviewed required set contains 11 unique names. Additional existing topics may remain only if they are accurate, non-duplicative, and keep the total within 20.

## Evidence Mapping

| Topic | Repository evidence |
| --- | --- |
| `brownfield` | README's primary product positioning and `examples/brownfield-bug-lab/` |
| `legacy-modernization` | README's .NET Framework 4.x and mixed MFC/managed scenarios |
| `code-analysis` | Roslyn syntax/semantic analysis and source-local candidate analyzers |
| `repository-analysis` | `inspect`, `investigate`, `evidence`, and `impact` commands |
| `spec-driven-development` | README SDD workflow and engine-neutral evidence handoff |
| `spec-kit` | `.specify/` integration and Spec Kit workbench |
| `dotnet` | `src/BrownPants.Cli/BrownPants.Cli.csproj` targets .NET 10 and analyzes older .NET projects |
| `csharp` | Roslyn C# parser and semantic target analysis |
| `visual-basic` | Roslyn VB syntax and semantic analyzer support |
| `cpp` | Native C/C++ source and C++/CLI lexical candidate analysis |
| `mfc` | MFC message-map and handler candidate tracing in `NativeFlowAnalyzer` |
