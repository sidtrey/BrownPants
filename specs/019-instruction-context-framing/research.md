# Research: Instruction Context Frames

## Decisions

### Keep source inventory in the deterministic core

The current `RepositoryScanner` and `SddWorkflowAnalyzer` discover project and SDD artifacts but do not map instruction conventions. Add a dedicated analyzer that returns repository-relative paths, recognition reasons, scope evidence, Markdown headings, byte counts, approximate token counts, and a content digest. This keeps existing inventory behavior focused and makes instruction claims independently reviewable.

### Treat convention discovery as evidence, not a complete host simulation

Recognize documented common names and directories (including `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.github/copilot-instructions.md`, `.github/instructions/`, and `.cursor/rules/`). Preserve configured path scopes when present and label inferred scope as heuristic. Allow explicit custom paths. No finite list can establish every host's effective prompt or precedence, so report recognized conventions and unsupported patterns.

### Do not generate semantic summaries in the core

The core has no LLM dependency. A portable frame is Markdown with short priorities and exact file/heading references, plus source digests and retrieval instructions. Semantic compression can be drafted by an SDD adapter or contributor, then validated against the discovered source map. Conflicts and uncited statements require review rather than automated reconciliation.

### Freeze acceptance before implementation

Store a SHA-256 digest of the feature's user scenarios/acceptance scenarios and success criteria. The verifier canonicalizes line endings, excludes unrelated spec sections, and fails on drift. Replacing a digest is an explicit reviewed operation; CI checks the current feature baseline, and Spec Kit guidance checks before and after implementation.

### Make writes opt-in and non-destructive

Inventory remains read-only. Frame creation requires an explicit output path under the repository, refuses an existing file unless an explicit overwrite option is supplied, and never edits source instructions or host configuration. Staleness compares recorded source digests against current content.

### State token estimates and host behavior honestly

UTF-8 bytes and a documented heuristic can be reported without model dependencies; label token estimates approximate. Whether a frame saves prompt tokens depends on how a host loads instruction files. The report must not equate a compact artifact with reduced host context.

## Alternatives Rejected

- Rewriting the twelve source instruction files: destroys canonical detail and risks changing host behavior.
- Automatically replacing root agent instructions with the generated frame: host semantics vary, and silent config edits violate safe defaults.
- Making an LLM mandatory for inventory or validation: breaks offline determinism and engine neutrality.
- Claiming universal token savings: the host may still inject all original files.
