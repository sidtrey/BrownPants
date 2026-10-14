# Research: Cross-Ecosystem Repository Inventory

## Decision: Classify from manifest paths

Use conventional manifest filenames and repository-relative paths to detect ecosystems. Do not parse package manifests, resolve dependency graphs, invoke package managers, or run build scripts. This keeps default inspection offline, deterministic, and safe for arbitrary source trees.

**Alternatives considered**: Parsing every ecosystem's manifest would introduce a growing dependency set and inconsistent error semantics. Shelling out to package/build tools could execute untrusted scripts and depends on installed toolchains. Path classification provides useful orientation without those costs.

## Decision: Add grouped systems without replacing existing markers

Retain `ProjectMarkers` for compatibility and add a grouped record containing ecosystem name plus all supporting manifest paths. A repository may contain multiple systems; a single path may support a broad build system and a language ecosystem.

**Alternatives considered**: Replacing marker paths with a single guessed primary language would lose mixed-repository evidence and break consumers. Grouped evidence preserves both detail and readable summaries.

## Decision: Report source-analysis coverage explicitly

Map common source extensions to language names and to the current analyzer level. C# and VB.NET report Roslyn syntax/candidate analysis; native C/C++ report lexical candidates; other recognized languages report inventory-only. Semantic resolution remains separately opt-in.

**Alternatives considered**: Treating every recognized ecosystem as parsed would overstate capability. Hiding unsupported languages would make BrownPants appear tied to .NET. Explicit coverage makes the gap visible and extensible.

## Decision: Count extensionless files

Represent extensionless files as `[no extension]`; retain unknown suffixes as their own extension rows. This makes language-neutral file counts reconcile with the total discovered file count.

## Existing Evidence

`RepositoryScanner` already counts extensions, detects a small set of manifests for .NET, Python, JavaScript, Go, Rust, JVM, Ruby, and PHP, and dispatches Roslyn C#/VB and lexical C/C++ analysis. The report currently does not group manifest paths by system, list source-language coverage, or count extensionless files. The test project uses temporary repositories and verifies deterministic Markdown/JSON output.
