# Quickstart: Retrieve One Instruction Section

## Prerequisites

- .NET 10 SDK
- A repository with a supported instruction file, such as `AGENTS.md`

## Discover available sections

```powershell
dotnet run --project src/BrownPants.Cli -- instructions . --format markdown
```

Use the path and anchor shown by the inventory to request one section:

```powershell
dotnet run --project src/BrownPants.Cli -- instructions . --source AGENTS.md --section build-and-test --format markdown
```

The result includes only that heading's section, with nested subsections retained, plus its source path and inclusive line range. Use `--format json` when a tool needs structured metadata and content.

## Validate behavior

```powershell
dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj --filter FullyQualifiedName~InstructionSourceAnalyzerTests
dotnet test BrownPants.sln
```

Verify Markdown and JSON describe the same text and source range. Also confirm missing anchors and unsafe paths fail without returning the full file.

## Host limitation

An SDD host must call this command or an adapter that calls it. Hosts may continue injecting all instruction files; a targeted result alone does not guarantee token savings.
