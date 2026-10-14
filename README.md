# BrownPants

BrownPants is a brownfield-first evidence layer for spec-driven development. It builds deterministic repository context that planning and implementation workflows can use to work from an existing codebase. The core inventory is independent of a particular SDD engine; it recognizes Spec Kit and OpenSpec artifacts and supports repository-declared roots for other engines, while engine adapters add workflow-specific guidance. The CLI's Markdown and JSON reports can also be consumed by other SDD engines. The scanner does not require an LLM.

## Quick start

Requires the .NET 10 SDK.

```powershell
dotnet build BrownPants.sln
dotnet run --project src/BrownPants.Cli -- inspect .
dotnet run --project src/BrownPants.Cli -- inspect . --format json
dotnet run --project src/BrownPants.Cli -- inspect . --format json --limit 1000
dotnet run --project src/BrownPants.Cli -- inspect . --semantic
dotnet pack src/BrownPants.Cli -c Release -o ./artifacts
dotnet tool install --tool-path ./.tools --add-source ./artifacts BrownPants.Cli --version 0.1.0
./.tools/brownpants inspect . # Windows: .\.tools\brownpants.exe inspect .
dotnet test BrownPants.sln
```

`inspect` reports project markers and grouped ecosystems, source-language coverage, file type counts, likely test paths, top-level entries, and supported SDD workflow artifacts when present. Spec Kit feature directories and OpenSpec capability/active-change directories are reported separately, with artifact paths bounded by `--limit`. When evidence is unambiguous, each workflow can also report an active work item: Spec Kit uses the validated `.specify/feature.json` pointer, OpenSpec selects only a single non-archived change directory, and custom workflows can declare an explicit path under their artifact roots. Multiple OpenSpec changes remain unselected, and invalid custom selections are diagnosed without discarding other workflow inventory. Ecosystem markers cover common .NET, C/C++, CMake/Make, Python, JavaScript/TypeScript, Go, Rust, JVM, Ruby, PHP, Apple, Dart, Elixir, Scala, container, and infrastructure files. Extensionless files and unfamiliar suffixes remain in file counts. The scanner skips common VCS, build, package, and environment directories and avoids following directory symlinks. Markdown is reviewable context for any planning workflow; JSON is available for tools and other SDD engines and starts with root `SchemaVersion: 1`. JSON consumers should check this integer before reading fields; it versions the report contract independently of the package. Optional additive fields remain version 1, while breaking field changes require a new schema version. Test-path signals remain heuristic, and project references are reported as written in project files rather than resolved into a dependency graph.

Language coverage identifies what BrownPants actually analyzes: Roslyn syntax and candidate patterns for C# and VB.NET, lexical candidate patterns for Python and native C/C++, and inventory-only coverage for other recognized source languages. Python candidates include factory class/method/call naming, Dependency Injector provider declarations, and dependency markers such as FastAPI `Depends`, `Provide[...]`, and `@inject`. Python analysis filters comments and string literals but does not parse Python or resolve runtime dependency graphs. Ecosystem detection comes from conventional manifest names; it does not prove that every listed project is active or buildable.

Roslyn parses C# and Visual Basic files without building the project, so legacy .NET Framework source can be inspected from a .NET 10 host. Reports include `.csproj` and `.vbproj` target framework values, project references, type/member declarations, import/using directives (C#), invocation syntax, inactive preprocessor text (C#), and parser diagnostics. Where a C# project has one explicit `LangVersion`, that version is used for its source files. Default mode reports syntax only: invocation names are not resolved to target methods, MSBuild conditions/imports and preprocessor symbols are not evaluated, and parser diagnostics are not build results. By default, structured symbol samples are capped at 200; raise the cap for JSON with `--limit 1000` (maximum 10000).

Pass `--semantic` to opt in to MSBuildWorkspace project loading. When the required MSBuild installation and references are available, BrownPants resolves C# and Visual Basic invocation expressions to method symbols and reports caller-to-target edges with source locations, along with project-load issues. This helps trace legacy behavior across methods while keeping the evidence reviewable. This mode may evaluate project imports and run design-time builds; review the repository before using it. It can be slower, results reflect MSBuild's loaded configuration, and unresolved calls do not necessarily mean broken code.

Native coverage includes `.vcxproj` and legacy `.vcproj` markers, MFC/CLR project settings, and source-located signals for MFC class inheritance from common framework types, runtime macros, object factories, message maps, COM activation and marshalling, COM interface metadata, C++/CLI, C#/VB interop attributes, factory types and calls, `Func<T>` factory delegates, constructor-injected interface/factory parameters, and DI APIs such as `AddTransient`, `RegisterType`, `RegisterInstance`, `Bind`, `GetRequiredService`, `GetInstance`, and container-scoped `Resolve`/`Get<T>`. Context checks on generic `Get`, `GetObject`, and `Resolve` names reduce matches from unrelated object APIs. Pattern findings carry `EvidenceKind: Candidate` in JSON and are labeled in Markdown; they are not proof of runtime relationships. C# and VB.NET patterns are syntax-based. Native C++ matches are lexical candidates, not a full C++ parse. Inspect both sides of managed/native boundaries and the cited build configuration.

## Workflow integrations

### Register another SDD engine

Add `.brownpants/sdd-workflows.json` to describe an engine that does not have a built-in detector:

```json
{
  "schemaVersion": 1,
  "workflows": [
    {
      "name": "ExampleSDD",
      "artifactRoots": ["planning"],
      "specificationRoots": ["planning/specs"],
      "changeRoots": ["planning/changes"],
      "archivedChangeDirectoryNames": ["archive"],
      "activeWorkItemPath": "planning/changes/add-search"
    }
  ]
}
```

`artifactRoots` is required; the other root lists and `activeWorkItemPath` are optional. When provided, `activeWorkItemPath` must be a safe directory below an artifact root and contain a scanned file; BrownPants reports it with `CustomWorkflowDeclaration` evidence. Invalid or stale selections add `SddWorkflowIssues` in JSON and diagnostics in Markdown while preserving the workflow's other counts. BrownPants matches roots and selections against files already discovered inside the repository; it does not enumerate configured roots, read selected directories, or execute engine code. The declaration must use schema version 1, contain no unknown properties, include at most 100 workflows, and remain under 1 MiB. See the [configuration contract](specs/005-pluggable-sdd-discovery/contracts/custom-workflow-config.md).

The core inventory is workflow-neutral. Spec Kit is the first adapter and refreshes BrownPants context before constitution, specify, clarify, plan, checklist, tasks, analyze, implement, and task-to-issues phases, then again after implementation for completion review. For Codex CLI, initialize Spec Kit with `specify init --here --integration codex --integration-options="--skills"`. Its commands are installed as `.agents/skills`; the BrownPants context command runs automatically through extension hooks. To install the BrownPants adapter, build the extension manifest with `integrations/speckit/BuildExtension.ps1`, install the BrownPants .NET tool, then run `specify extension add <extension-path> --dev`.

For OpenSpec with Codex, initialize with `openspec init --tools codex`, install BrownPants, and run `integrations/openspec/Install.ps1 -ProjectRoot .`. Invoke `$openspec-brownpants-context` before `$openspec-propose` or `$openspec-apply-change`; OpenSpec does not provide the Spec Kit lifecycle hooks, so this adapter is explicit. See [the OpenSpec adapter guide](integrations/openspec/README.md). Other SDD engines can consume the same JSON/Markdown evidence; engine-specific adapters belong under `integrations/` and should not change scanner semantics.

## Development

See [AGENTS.md](AGENTS.md) for repository contribution guidance. Keep analysis deterministic, document evidence limits, and add fixture-based tests for new inventory behavior. GitHub Actions runs the Release build, xUnit and adapter checks, then packs and smoke-tests the .NET tool on Windows, Linux, and macOS. BrownPants inventories Windows-specific project metadata on every host; it does not build MFC projects as part of inspection.
