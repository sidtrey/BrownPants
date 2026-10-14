# Tasks: Cross-Ecosystem Repository Inventory

**Input**: Design documents in `specs/002-cross-ecosystem-inventory/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/project-inventory.md`, `quickstart.md`

**Tests**: Fixture-based xUnit coverage is required by the feature's measurable outcomes and user request.

**Organization**: Tasks are grouped by independently reviewable user story. Write tests before each story's implementation.

## Phase 1: Setup

**Purpose**: Existing CLI, Spec Kit, and xUnit infrastructure is in place.

- [X] T001 Confirm existing report and fixture seams in `RepositoryScanner`, `RepositoryInventory`, `MarkdownReport`, and `RepositoryScannerTests`.

## Phase 2: User Story 1 - Orient in any repository (Priority: P1)

**Independent Test**: A mixed temporary repository inventories manifests, unknown suffixes, and extensionless files with deterministic totals.

- [X] T002 Add fixture coverage for extensionless files, unknown extensions, cross-ecosystem generated/dependency directories, manifest paths, and grouped project systems in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T003 Include extensionless files in extension counts and add grouped project-system records in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs` and `RepositoryScanner.cs`.
- [X] T004 Add deterministic ecosystem manifest classification for .NET, C/C++, Python, JavaScript/TypeScript, Go, Rust, JVM, Ruby, PHP, Apple, Dart, Elixir, Scala, container, and infrastructure project markers in `src/BrownPants.Cli/Analysis/ProjectSystemAnalyzer.cs`.

## Phase 3: User Story 2 - Understand source-analysis coverage (Priority: P1)

**Independent Test**: A mixed-language fixture reports actual parser coverage for C#/VB/C++ and inventory-only coverage for other recognized source languages.

- [X] T005 Add fixture assertions for language names, file counts, analysis levels, and deterministic JSON serialization in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T006 Map common source extensions to stable language labels and actual analysis levels in `src/BrownPants.Cli/Analysis/SourceLanguageCoverageAnalyzer.cs`; expose records in `RepositoryInventory.cs`.
- [X] T007 Render ecosystem and language coverage sections from inventory records in `src/BrownPants.Cli/Output/MarkdownReport.cs` while preserving JSON/Markdown parity.

## Phase 4: User Story 3 - Trust the inventory limits (Priority: P2)

**Independent Test**: Reports describe inventory-only languages accurately and do not imply their source was parsed or projects were built.

- [X] T008 Update `README.md`, `specs/002-cross-ecosystem-inventory/contracts/project-inventory.md`, and Spec Kit context guidance with manifest scope, analysis levels, and offline limits.
- [X] T009 Verify repeated Markdown/JSON output, `dotnet test BrownPants.sln`, Release package creation, and a local CLI inspection.

## Dependencies

- T002 precedes T003-T004. T005 precedes T006-T007. T008 follows the report model and renderer. T009 follows all stories.
- User Story 1 and User Story 2 are independently testable after the existing CLI foundation; User Story 3 documents their evidence boundaries.

## Implementation Strategy

Deliver repository-wide manifest and file inventory first, then report source-language coverage, then update user guidance. Preserve the existing C#/VB Roslyn and MFC/native analyzers. Do not add ecosystem parsers or execute project tools as part of this feature.
