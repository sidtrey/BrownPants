# Implementation Plan: JavaScript and TypeScript Source Evidence

**Branch**: `072-ts-source-evidence` | **Date**: 2026-10-16 | **Spec**: [spec.md](spec.md)

## Summary

Promote JavaScript/TypeScript from lexical factory candidates to useful, bounded, source-located syntax evidence for declarations, modules, and calls. Resolve only deterministic in-repository relationships supported by explicit workspace and TypeScript configuration. Preserve 014 candidates and clearly separate syntax resolution from compiler binding.

## Technical Context

- **Language**: C# / .NET 10; input formats: JavaScript and TypeScript.
- **Dependencies**: Existing .NET libraries only; do not add packages.
- **Storage**: Read-only source and bounded JSON configuration under the scan root.
- **Testing**: xUnit temporary repositories under `tests/BrownPants.Tests`.
- **Constraints**: Offline, no package installation, no code execution, deterministic ordering, explicit limits, safe path resolution.

## Constitution Check

- Evidence First: distinguish syntax-resolved from compiler-bound and unresolved claims. PASS.
- Broad, Engine-Neutral Core: report in common inventory formats. PASS.
- Deterministic and Reviewable Output: bounded samples, stable ordering and exact source locations. PASS.
- Safe Inspection by Default: parse only files under the scan root; no external reads or execution. PASS.
- Specified and Verified Changes: hashed ACs, fixture-first tasks, full release checks. PASS.

## Design

Add a bounded tokenizer and syntax-oriented recognizers rather than regular-expression-only matching. Parse supported declarations, static import/export forms and calls while masking non-code text and retaining offsets. Add guarded resolvers for relative module paths, declared workspace package paths, and `tsconfig` baseUrl/path aliases. Model calls as syntax-resolved only when a unique declaration remains; preserve ambiguity and unresolved reasons. Add additive inventory records and matching Markdown sections. Keep scanner budgets centralized and report truncation. Extend existing 014 coverage and docs. Do not claim standards-complete ECMAScript/TypeScript parsing; unsupported syntax must be represented as a limit or unresolved record.

## Validation

Run acceptance hash and ledger checks; focused JS/TS tests; full Release build and xUnit suite; Spec Kit/OpenSpec adapter checks; package and installed-tool smoke checks; inspect generated JSON/Markdown and exercise BrownPants-on-BrownPants. Record actual results and retain failure evidence.
