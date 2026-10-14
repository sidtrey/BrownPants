# Research: Managed and Native Interop Analysis

## Decision: Keep default analysis static and build independent

**Rationale**: BrownPants already parses C# and VB.NET with Roslyn without compiling target projects, reads native project metadata, and has fixture-based tests. Legacy target frameworks and unavailable Windows toolchains are common constraints. A default scan should continue to work from readable files alone.

**Alternatives considered**: Requiring a successful MSBuild load would exclude many older projects. Existing `--semantic` mode remains an opt-in for users who trust project imports and need symbol-resolved C#/VB call edges.

## Decision: Represent native C++ pattern matches as candidates

**Rationale**: Current native detection strips comments and strings, then recognizes MFC macros, COM activation, C++/CLI syntax, factory-like methods, and smart-pointer construction lexically. It does not parse C++ grammar, resolve macros, or determine runtime targets. Reports must preserve this evidence limit.

**Alternatives considered**: Claiming resolved call edges from regex matches would overstate what the analyzer proves. Adding a full native compiler front end is outside this feature's repository-local scope.

## Decision: Use source locations and stable report ordering as the review contract

**Rationale**: Findings need to be checked in large legacy repositories, so repository-relative paths, line numbers, bounded samples, deterministic order, and Markdown/JSON parity make evidence actionable and reproducible.

**Alternatives considered**: Summaries without source locations are too weak for follow-up; unbounded output can make reports unwieldy.

## Decision: Keep the Spec Kit integration as a context provider

**Rationale**: BrownPants should provide evidence before specify/plan and should not rewrite a user's feature request or application code. The existing context command describes this boundary and can be extended as report fields evolve.

**Alternatives considered**: Automatically generating design decisions from pattern counts would turn uncertain source evidence into requirements and architecture claims.

## Repository findings

- `RepositoryScanner` coordinates language and project analyzers.
- `CSharpAnalyzer` and `VisualBasicAnalyzer` provide syntax-level inventories; `CSharpSemanticAnalyzer` loads projects only in opt-in semantic mode.
- `NativeProjectAnalyzer` reads `.vcxproj` and `.vcproj` metadata including MFC/CLR settings.
- `ArchitecturePatternAnalyzer` uses Roslyn syntax for C# and VB.NET, and comment/string-stripped lexical checks for native source. Native C++ raw strings are tracked across line boundaries so literal examples do not become code signals.
- `MarkdownReport` formats human-readable evidence; `Program` serializes the inventory records for JSON.
- `tests/BrownPants.Tests/RepositoryScannerTests.cs` uses temporary repository fixtures to verify legacy projects, pattern evidence, false-positive exclusion, output, and determinism.
