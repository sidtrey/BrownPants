# Implementation Plan: Compiler-Backed JavaScript and TypeScript Syntax Checks

**Branch**: `075-compiler-backed-js-ts-syntax` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

## Summary

Add an explicit `inspect --typescript-syntax` option. It discovers an existing TypeScript compiler package under the scanned root, invokes Node with a fixed parser-only adapter, and reports syntax diagnostics separately from the bounded recognizer's heuristic findings. Default inspection also recognizes React dependencies in package manifests. With parser verification enabled, AST traversal adds React function-component and hook-call candidates with source locations. Default inspection remains process-free. A missing runtime/compiler produces an explicit unavailable state rather than a download or fallback claim of parser validation.

## Technical Context

**Language/Version**: C# on .NET 10; TypeScript parser API from the local package selected by the scanned repository

**Primary Dependencies**: Existing .NET dependencies; Node.js and a repository-local `typescript` package only for the explicit option

**Storage**: N/A; parser input is sent through standard input and no temporary file is created

**Testing**: xUnit unit tests for discovery, path containment, fake-verifier result mapping, statuses and default behavior; full BrownPants suite; live parser verification against FullMetalFile's installed TypeScript 5.9 package

**Target Platform**: Windows, Linux, and macOS with .NET 10; parser verification additionally needs Node.js and the local compiler package

**Project Type**: .NET CLI

**Performance Goals**: One Node process per distinct compiler package; bounded by the existing source-file and source-size analysis caps and a fixed process timeout

**Constraints**: Never run package scripts, type-check, build, or load project configuration. Do not use network. Default mode does not start a process. The compiler package itself is trusted executable code, so the explicit option and resolved compiler path/version must be visible.

**Scale/Scope**: JS/JSX/TS/TSX and their module variants already recognized by BrownPants; diagnostics and statuses remain deterministically sorted and bounded by the existing sample limit.

## Constitution Check

- **Evidence First**: Compiler syntax errors are separated from recognizer candidates; no type-check or build claims are made.
- **Broad, Engine-Neutral Core**: Node/TypeScript is optional and only affects JS/TS evidence.
- **Deterministic and Reviewable Output**: Stable compiler discovery, sorting, output, limits, and paths.
- **Safe Inspection by Default**: No runtime launch by default; explicit opt-in loads only the discovered local parser and sends only source paths. Node environment injection variables are removed; project source and package scripts are never evaluated.
- **Specified and Verified Changes**: Fixture coverage, both output formats, and a live FullMetalFile proof.

No constitution violations identified.

## Design

### Parser discovery and invocation

For each `package.json` discovered under the canonical scan root, check for the adjacent `node_modules/typescript/lib/typescript.js`. Resolve the nearest applicable candidate to each source file by directory distance, reject paths outside the scan root and reparse-point components, and read the adjacent package version. Do not consult package-manager commands, global packages, `NODE_PATH`, or `NODE_OPTIONS`. Resolve `node` from `PATH` only after the user selected parser verification.

Group JS/JSX/TS/TSX files by compiler path and invoke one Node process for each package. Use a fixed inline adapter passed as an argument and a JSON request on standard input. The adapter loads the selected TypeScript package, reads only enumerated in-root source paths, calls `createSourceFile` with the matching script kind and latest language target, and returns `parseDiagnostics`, parser version, and per-file status. It never calls the type checker or project APIs. Use `ArgumentList`, no shell; clear `NODE_OPTIONS` and `NODE_PATH`; enforce a timeout and bounded output. Compiler/library execution is the explicit trust boundary and is named in output.

### Analysis and report model

Extend `JavaScriptTypeScriptAnalysis` with a nullable syntax-verification result and React-support evidence. React project detection reads bounded `package.json` manifests for React dependencies during every scan. It is local metadata evidence, not proof that the project builds or runs. The opt-in parser result contains requested state, sorted compiler identities, per-file statuses, compiler diagnostics (path, code, message, line, column), and heuristic findings. For detected React projects, AST traversal reports function declarations/arrow-function variables containing JSX and call expressions named `use` followed by an uppercase character as component/hook candidates. These are syntax candidates; import binding and React runtime rules are not resolved.

When a file is parsed, move lexical parse-shape issue codes for that file out of the generic analysis issue list and into the heuristic findings section. This prevents heuristic warnings from being presented as compiler failures while preserving them for review. Files without a usable compiler retain their existing lexical issues and show unavailable/failed status. Resource-cap and read errors remain analysis issues.

Markdown and JSON expose the same state. Existing report output and JSON shape remain behaviorally unchanged when the flag is omitted, except for additive model fields with a null default.

### CLI contract

Add `--typescript-syntax` to `inspect`. Reject it with a clear error if passed to another command (other commands already reject unknown options). Help text must explain that it loads a repository-local TypeScript parser and does not type-check or build the project. Missing `node` or package is a reported unavailable state, not a nonzero scan failure.

### Tests and evidence

- Keep scanner tests independent of Node by injecting a fake parser verifier into the analyzer seam.
- Test nearest-version selection, deterministic grouping, no compiler, fake-verifier result mapping, path escape and reparse rejection, sorting, and unchanged default behavior. The live FullMetalFile run exercises the actual Node/TypeScript parser process; process timeout handling is implemented but not directly tested.
- Add fixtures for valid TSX/JSX, nested template literals, regex literals, malformed delimiter/string syntax, React manifest declaration, component, and hook candidates.
- Exercise Markdown/JSON parity and stable repeated output.
- Run the actual TypeScript 5.9 API on FullMetalFile production TS/TSX, and Node syntax parse on `scripts/authz-matrix.mjs`; compare BrownPants' parser diagnostics to the compiler API result.

## Project Structure

```text
src/BrownPants.Cli/
├── Analysis/
│   ├── JavaScriptTypeScriptAnalyzer.cs
│   ├── JavaScriptTypeScriptModels.cs
│   ├── ReactProjectAnalyzer.cs
│   └── TypeScriptSyntaxVerifier.cs
├── Output/MarkdownReport.cs
└── Program.cs

tests/BrownPants.Tests/
└── RepositoryScannerTests.cs

specs/075-compiler-backed-js-ts-syntax/
├── spec.md
├── plan.md
├── research.md
├── quickstart.md
└── tasks.md
```

**Structure Decision**: Extend the existing analyzer, inventory record, report formatter, and inspect command. Keep the process and package discovery logic in a dedicated analysis helper; test through an injected verifier seam.

## Complexity Tracking

No constitution exceptions. The optional Node boundary is justified because a standards-complete TSX parser is not available in the current dependency-free .NET core; it is isolated behind an explicit CLI option and has a bounded fallback.
