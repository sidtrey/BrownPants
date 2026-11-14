# Implementation Plan: Indirect Placeholder Accessors

1. Extend the C# placeholder candidate model with optional delegated-helper evidence while preserving existing JSON fields.
2. Analyze each containing type for direct `NotImplementedException` helper bodies, then detect one-hop expression bodies on methods and property getters that invoke those helpers.
3. Keep direct candidates, indirect candidates, signatures, source ordering, sample limits, and read limits deterministic.
4. Add Roslyn fixture tests for supported forms and negative cases; assert Markdown/JSON parity and backward-compatible direct findings.
5. Build and test BrownPants, run a parser-backed FullMetalFile inventory, and verify all five PostgreSQL unit-of-work accessors appear.

## Constraints

- Do not perform dataflow, inheritance, overload, or runtime-dispatch analysis.
- Do not modify FullMetalFile application behavior as part of this BrownPants feature; use its existing unsupported accessors as a scanner proof target.
- Preserve any pre-existing working-tree changes.
