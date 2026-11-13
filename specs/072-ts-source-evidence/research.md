# Research: JavaScript and TypeScript Source Evidence

## Existing repository behavior

- `ArchitecturePatternAnalyzer` contains a line-oriented lexical scanner for JS/TS factory and DI candidates. It masks comments and some strings, but does not parse declarations or calls.
- `SourceLanguageCoverageAnalyzer` currently labels JS/TS as lexical candidates.
- `RepositoryScanner` builds the inventory, and `MarkdownReport` renders it. JSON serialization is additive and the root inventory schema is version 1.
- Default inspection is offline and read-only; `node_modules` and generated output are excluded.

## Decisions

1. Use a dedicated tokenizer/parser surface in C# and no new dependency. A Node/TypeScript compiler would require a runtime/package installation and would contradict the core's offline inspection promise.
2. Retain token offsets and line/column during scanning. Mask comments and literal contents for name detection while preserving JSX expression regions; mark syntax outside supported recognizers unresolved rather than applying broad regex matches.
3. Model declarations, module edges, call sites, and evidence status as separate additive records. Existing feature 014 candidates remain unchanged for compatibility and are regression-tested.
4. Resolve paths only after canonicalizing beneath the root. Read only conventional JS/TS file variants, package manifests, and tsconfig files already discovered under the root. Reject root escapes, symlinks/reparse points, malformed JSON, and ambiguous matches.
5. Resolve extensionless/index relative modules, declared workspace package roots, and tsconfig `baseUrl`/`paths` patterns with one wildcard capture. Do not guess `package.json` conditional exports or TypeScript overload binding.
6. Use the user-selected sample limit for each emitted record family; retain exact totals and omission counts. Sort by repository path, source offset, then stable symbol text.

## Risks and mitigations

- JavaScript/TypeScript grammar evolves and is too broad for a small custom parser. Keep recognizers bounded, create adversarial fixtures, expose syntax limits, and avoid claiming complete parsing.
- Regex literals and JSX make slash/angle-bracket tokenization ambiguous. Use prior-token context and a focused fixture corpus; where classification is uncertain, suppress call records rather than inventing them.
- Monorepos can have multiple tsconfig/package roots. Resolve configuration nearest to source first, and preserve ambiguity instead of selecting the first match.
- Large or hostile config/source files could consume resources. Apply explicit file-size and token/relationship budgets consistent with scanner conventions and expose truncation.
