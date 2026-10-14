# Repository Guidelines

## Project Structure & Module Organization

`src/BrownPants.Cli/` contains the .NET 10 command-line application. `Analysis/` holds deterministic filesystem, project-file, and Roslyn C# analysis; `Output/` formats reports. `tests/BrownPants.Tests/` contains xUnit tests and temporary filesystem fixtures. Keep test data generated in test code unless a fixture needs to be reviewed as a standalone file.

## Build, Test, and Development Commands

- `dotnet build BrownPants.sln` builds the CLI and tests.
- `dotnet test BrownPants.sln` runs the xUnit suite.
- `dotnet run --project src/BrownPants.Cli -- inspect .` inspects the current directory as Markdown.
- Add `--format json` for machine-readable inventory.
- Add `--limit 1000` to retain larger Roslyn symbol samples (valid range: 1–10000).
- Add `--semantic` to opt into MSBuildWorkspace and semantic call-target resolution; project evaluation may execute imported build targets.

## Coding Style & Naming Conventions

Use four spaces, nullable reference types, PascalCase for public types/members, and camelCase for locals and parameters. Keep scanners deterministic: sort output, avoid timestamps and network calls, and state evidence limits. Do not claim source-level relationships until they are actually parsed and validated.

## Testing Guidelines

Use xUnit with descriptive `Method_ExpectedBehavior` test names. Test against small temporary repositories, including ignored build/VCS directories, Spec Kit files, legacy `.csproj` metadata, C# syntax and semantic resolution, and invalid inputs. Run `dotnet test BrownPants.sln` before submitting changes.

## Commit & Pull Request Guidelines

Git metadata is not available in this checkout, so no existing commit convention can be confirmed. Use concise imperative subjects such as `Add deterministic repository inventory`. PRs should describe behavior and evidence, list build/test results, link relevant issues or design notes, and include representative report output when it helps review.

## Security & Configuration

Do not commit credentials, private repository contents, or machine-specific settings. Inventory reports can expose repository paths and project structure; review generated reports before sharing them externally.
