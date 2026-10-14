# BrownPants

BrownPants is a brownfield-first code intelligence tool. It builds deterministic repository context so Spec Kit workflows can plan against evidence in an existing codebase. The initial CLI does not require an LLM.

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

`inspect` reports known project markers, file type counts, likely test paths, top-level entries, and Spec Kit artifacts. It skips common VCS, build, package, and environment directories and avoids following directory symlinks. Markdown output is intended as reviewable context for a Spec Kit planning session; JSON is available for tools. Test-path signals remain heuristic, and project references are reported as written in project files rather than resolved into a dependency graph.

Roslyn parses C# and Visual Basic files without building the project, so legacy .NET Framework source can be inspected from a .NET 10 host. Reports include `.csproj` and `.vbproj` target framework values, project references, type/member declarations, import/using directives (C#), invocation syntax, inactive preprocessor text (C#), and parser diagnostics. Where a C# project has one explicit `LangVersion`, that version is used for its source files. Default mode reports syntax only: invocation names are not resolved to target methods, MSBuild conditions/imports and preprocessor symbols are not evaluated, and parser diagnostics are not build results. By default, structured symbol samples are capped at 200; raise the cap for JSON with `--limit 1000` (maximum 10000).

Pass `--semantic` to opt in to MSBuildWorkspace project loading. When the required MSBuild installation and references are available, BrownPants resolves C# and Visual Basic invocation expressions to method symbols and reports caller-to-target edges with source locations, along with project-load issues. This helps trace legacy behavior across methods while keeping the evidence reviewable. This mode may evaluate project imports and run design-time builds; review the repository before using it. It can be slower, results reflect MSBuild's loaded configuration, and unresolved calls do not necessarily mean broken code.

Native coverage includes `.vcxproj` and legacy `.vcproj` markers, MFC/CLR project settings, and source-located signals for MFC runtime macros, object factories, message maps, COM activation and marshalling, COM interface metadata, C++/CLI, C#/VB interop attributes, factory types and calls, `Func<T>` factory delegates, constructor-injected interface/factory parameters, and common DI registration and resolution calls. Pattern findings carry `EvidenceKind: Candidate` in JSON and are labeled in Markdown; they are not proof of runtime relationships. C# and VB.NET patterns are syntax-based. Native C++ matches are lexical candidates, not a full C++ parse. Inspect both sides of managed/native boundaries and the cited build configuration.

## Spec Kit workflow

For Codex CLI, initialize Spec Kit with `specify init --here --integration codex --integration-options="--skills"`. Its commands are installed as `.agents/skills` and invoked as `$speckit-specify`, `$speckit-plan`, `$speckit-tasks`, and `$speckit-implement`. Run `brownpants inspect <repository>` before planning. To add the BrownPants context command and automatic specify/plan hooks, first build the extension manifest with `integrations/speckit/BuildExtension.ps1`, install the BrownPants .NET tool, then run `specify extension add <extension-path> --dev`. BrownPants grounds Spec Kit's specify → plan → tasks → implement → converge cycle in repository evidence.

## Development

See [AGENTS.md](AGENTS.md) for repository contribution guidance. Keep analysis deterministic, document evidence limits, and add fixture-based tests for new inventory behavior.
