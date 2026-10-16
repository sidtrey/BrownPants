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

The report gives exact totals and bounded samples with repository-relative paths and one-based lines/columns. Omitted records and analysis issues are counted. Results are deterministic and read-only; BrownPants does not run Node, install packages, execute scripts, or access the network.

This is bounded syntax analysis rather than a TypeScript compiler. It does not prove type correctness, bind overloads, select conditional package exports, or establish runtime dispatch, module loading, or execution. Existing JavaScript/TypeScript factory and dependency-injection matches remain lexical candidates.
