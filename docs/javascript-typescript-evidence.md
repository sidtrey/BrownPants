# JavaScript and TypeScript evidence

BrownPants inspects `.js`, `.jsx`, `.mjs`, `.cjs`, `.ts`, `.tsx`, `.mts`, and `.cts` files during ordinary offline `inspect` runs. The report adds a **JavaScript and TypeScript source evidence** section with source-located declarations, static imports and re-exports, and bounded call relationships.

```powershell
brownpants inspect . --format markdown
brownpants inspect . --format json --limit 1000
```

For example, importing `createOrder` from `../orders/create-order.js` can point to `orders/create-order.ts` when that is the only in-repository match. A configured `tsconfig` path such as `@app/*` can point to its matching file. Declared npm or pnpm workspace packages are considered when their package manifests and source files are inside the scan. A barrel export is followed when its static re-export path and exported name are unambiguous.

Relationship labels tell you how much the source proves:

- `syntax-resolved` identifies one supported in-repository target by static source rules.
- `ambiguous` lists possible source targets when multiple declarations or modules match.
- `unresolved` includes a reason for dynamic imports, external packages, missing files, unsupported configuration, and other cases BrownPants cannot safely settle.

The report gives exact totals and bounded samples with repository-relative paths and one-based lines/columns. Omitted records and analysis issues are counted. `ScannedFileCount` counts source files attempted, including the file that reaches an aggregate source or token limit. Results are deterministic and read-only; BrownPants does not run Node, install packages, execute scripts, or access the network.

For large repositories, analysis is capped at 20,000 source files and 20,000 workspace/configuration files. Workspace metadata is limited to 1 MiB per file and 32 MiB total. Source text is limited to 10 MiB per file and 100 MiB total; token work is limited to 1,000,000 tokens per file and 5,000,000 total. The analyzer retains at most 100,000 declaration, module, or call records per category and 10,000 diagnostics. Reached caps appear as analysis issues, and reported record or diagnostic totals become lower bounds when their cap is reached.

This is bounded syntax analysis rather than a TypeScript compiler. It resolves `this.method()` only to a unique method declared in the same class body and avoids resolving imported calls shadowed by recognized parameters or local variables. It does not prove type correctness, bind overloads, select conditional package exports, or establish runtime dispatch, module loading, or execution. Existing JavaScript/TypeScript factory and dependency-injection matches remain lexical candidates.
