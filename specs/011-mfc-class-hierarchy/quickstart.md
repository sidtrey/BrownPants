# Quickstart: Verify MFC Class Inheritance Signals

1. Run `dotnet build BrownPants.sln`.
2. Run `dotnet test BrownPants.sln` and review the temporary native source fixtures.
3. Run `dotnet run --project src/BrownPants.Cli -- inspect . --format markdown --limit 50` and inspect any `MFC class inheritance` samples.
4. Run the same command with `--format json` and confirm the category, `Candidate` evidence kind, language, path, and declaration line agree with Markdown.
5. Treat every result as a lexical candidate and inspect the cited C++ declaration and project configuration directly.
