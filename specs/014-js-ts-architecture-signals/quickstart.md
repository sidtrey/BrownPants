# Quickstart: Verify JavaScript and TypeScript Factory and DI Signals

1. Run `dotnet build BrownPants.sln` and `dotnet test BrownPants.sln`.
2. Inspect the temporary `.js` and `.ts` fixtures in Markdown and JSON.
3. Confirm the same candidate category, language, path, line, evidence kind, and evidence appear in each format.
4. Confirm comments, quoted strings, template literals, and unrelated calls produce no signals.
5. Confirm JavaScript and TypeScript coverage is labeled `Lexical candidate patterns` and repeated output is byte-identical.
