# Quickstart: Cross-Ecosystem Inventory

Run the test suite:

```powershell
dotnet test BrownPants.sln
```

Inspect the current repository in either format:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect .
dotnet run --project src/BrownPants.Cli -- inspect . --format json
```

Review `Project ecosystems` / `ProjectSystems` for manifest paths grouped by system. Review `Language coverage` / `LanguageCoverage` to see which source languages receive syntax or lexical analysis and which are inventoried only. Confirm extensionless and unknown files appear in aggregate counts. The scan must not invoke ecosystem build tools.

Fixture verification should include a mixed repository containing .NET/MFC, Python, JavaScript/TypeScript, Go, Rust, JVM, Ruby, PHP, Apple, Dart, CMake/Make/Meson, and container/infrastructure manifests, plus an unknown extension and an extensionless file. Add representative generated/cache folders such as `node_modules`, `.gradle`, `.next`, `.terraform`, and `.dart_tool` and verify they remain excluded. Compare Markdown and JSON records and run the same scan twice to confirm stable output.
