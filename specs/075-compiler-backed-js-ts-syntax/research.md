# Research: Compiler-Backed JavaScript and TypeScript Syntax Checks

## Findings

1. **The current analyzer is intentionally heuristic.** `JavaScriptTypeScriptAnalyzer` tokenizes common constructs and runs `ValidateDelimiters`; its evidence limits state it is not a standards-complete parser. The CLI currently invokes it in every `RepositoryScanner.Scan` and has no option to request a JS/TS parser.
2. **A real parser is already present in FullMetalFile.** `FullMetalFile.Client/package.json` declares TypeScript `~5.9.3`. Its installed `typescript.js` exports `createSourceFile`, which returns `parseDiagnostics`; Node's syntax parser accepts `scripts/authz-matrix.mjs`.
3. **The tested FullMetalFile discrepancy is reproducible.** BrownPants reports 14 JS/TS syntax warnings (11 across nine production source/script files and three under `.local`). TypeScript's parser reports no diagnostics for all eight production TS/TSX files, and `node --check` accepts the MJS script.
4. **Type checking is outside scope.** The parser adapter uses source-file parsing only. It does not load `tsconfig`, resolve modules, evaluate package scripts, type-check, or build.
5. **Execution must be opt-in.** Requiring the local TypeScript module executes compiler package code. The user chooses this through a dedicated option; BrownPants displays the resolved path and version and does not download or install anything.
6. **React is declared in the existing package manifest.** `FullMetalFile.Client/package.json` declares React `^19.2.7`. The client includes function components such as `App` and `SearchPage` and hook calls such as `useState` and `useEffect`; these can be navigated from AST locations without claiming runtime behavior.

## Decisions

- Use a repository-local `node_modules/typescript/lib/typescript.js`, selected nearest to each source file and bounded by the scan root.
- Discover packages from the scanned `package.json` paths, so source files such as root-level scripts can use the closest installed workspace parser even if it is in a sibling package.
- Invoke Node once per distinct compiler path with a fixed parser adapter, JSON on standard input, and no shell or temporary files.
- Remove `NODE_OPTIONS` and `NODE_PATH` for the child process, never invoke package-manager scripts, and time-limit the parser.
- Keep the default scanner process-free. No compiler/runtime means explicit unavailable status and the existing heuristic evidence.
- Detect React projects from the `react` dependency in bounded package manifests on default scans; emit component and hook-call source candidates only when opt-in parser verification is active.
- Separate heuristic lexical findings from parser diagnostics when parser verification succeeds.

## Open Risks

- A malicious or modified local TypeScript package is executable when explicitly selected; showing its path/version and requiring opt-in communicates the trust boundary but cannot sandbox arbitrary package code.
- The parser's `Latest` target checks syntax accepted by that package version, not the project's configured `target`, `lib`, or type correctness.
- Node process startup and parser loading add latency; grouping by compiler path limits this to one process per distinct package.
