# Research: Bug Investigation Context

## Decision: Reuse repository discovery rules

**Decision**: Factor discovered-file enumeration into a shared scanner method consumed by both `inspect` and the new investigation command.

**Rationale**: The current scanner already sorts repository-relative paths, skips generated/build/VCS directories, and avoids directory symlinks. Duplicating those rules would make reports diverge over time.

**Alternatives considered**: Re-enumerating independently in the investigator was rejected because duplicate traversal logic can drift. Extending `RepositoryInventory` with every file path was rejected because it would unnecessarily expand the existing inspect JSON contract and retain a large duplicate sample.

## Decision: Explicit literal clues

**Decision**: Accept one or more user-supplied clue strings and match each literally, case-insensitively, in file paths and file lines.

**Rationale**: Explicit clues keep the results explainable and deterministic across languages, including inventory-only languages. Natural-language token extraction would require ambiguous stop-word, stemming, and relevance rules that could bury useful matches.

**Alternatives considered**: Automatic extraction and ranked relevance were deferred because they would imply semantic precision BrownPants cannot currently establish. Framework-specific query logic was rejected because bug evidence spans mixed repositories.

## Decision: Separate, bounded report contract

**Decision**: Give investigation output its own versioned JSON model and Markdown renderer, with match counts, bounded samples, test-path classification, match kinds, and diagnostics.

**Rationale**: A separate report protects the existing inspect schema. Reporting locations without source snippets reduces unintended disclosure when users paste context into an SDD workflow.

**Alternatives considered**: Adding optional fields to the inspect schema would couple investigation behavior to every regular scan and broaden the existing contract. Returning matching line text was rejected because reports can be shared outside the repository.

## Decision: Read-only behavior

**Decision**: Inspect discovered files only. Do not run tests, builds, source code, or network requests.

**Rationale**: This matches the constitution's safe default and keeps the operation useful on repositories whose dependencies or platform toolchains are unavailable.
