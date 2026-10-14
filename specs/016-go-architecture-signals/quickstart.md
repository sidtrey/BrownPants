# Quickstart: Verify Go Architecture Signals

1. Run `dotnet build BrownPants.sln` and `dotnet test BrownPants.sln`.
2. Scan temporary Go fixtures containing constructor functions, call sites, and Fx/Dig wiring.
3. Confirm signals retain category, language, repository-relative path, line, and `Candidate` evidence in Markdown and JSON.
4. Confirm comments, quoted strings, rune literals, and multiline raw strings produce no matches.
5. Confirm Go coverage is labeled `Lexical candidate patterns` and repeated output is byte-identical.
