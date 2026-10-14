# Quickstart: Versioned JSON Report

## Prerequisites

- .NET 10 SDK

## Validate

1. Run `dotnet run --project src/BrownPants.Cli -- inspect <repository> --format json`.
2. Parse the output as JSON and confirm root `SchemaVersion` is the integer `1`.
3. Repeat against an empty temporary repository and a repository with SDD workflow declarations; the version remains `1`.
4. Run `dotnet run --project src/BrownPants.Cli -- inspect <repository>` and confirm the Markdown report has no schema marker.
5. Run `dotnet test BrownPants.sln`.

## Expected result

Every successful JSON inventory has the same root version and all existing inventory fields remain present. Markdown output remains human-focused and unchanged by the schema version.

