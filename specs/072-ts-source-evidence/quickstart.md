# Quickstart: JavaScript and TypeScript Source Evidence

Run the ordinary offline scan:

```powershell
dotnet run --project src/BrownPants.Cli -- inspect <repository> --format markdown
dotnet run --project src/BrownPants.Cli -- inspect <repository> --format json --limit 1000
```

Review the JavaScript/TypeScript source evidence section for declarations, static module relationships and bounded calls. A `syntax-resolved` edge identifies one source declaration under supported static rules; `ambiguous` lists possible in-repository targets; `unresolved` includes a reason such as dynamic import, unsupported alias, missing module, external package, or runtime dispatch. Check the omitted counts before relying on samples.

Inspection does not run Node, install packages, execute project scripts, type-check the program, or resolve conditional package exports. Existing lexical factory and DI signals remain separate candidate evidence.
