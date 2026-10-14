# Quickstart: Verify JVM Architecture Signals

1. Run `dotnet build BrownPants.sln` and `dotnet test BrownPants.sln`.
2. Scan temporary `.java`, `.kt`, and `.kts` fixtures in Markdown and JSON.
3. Confirm factory and DI candidates retain category, language, relative path, line, and `Candidate` evidence.
4. Confirm comments, Java text blocks, Kotlin raw strings, and unrelated annotations do not produce matches.
5. Confirm Java and Kotlin coverage is labeled `Lexical candidate patterns` and output is deterministic.
