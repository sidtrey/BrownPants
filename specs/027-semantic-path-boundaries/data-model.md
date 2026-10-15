# Data Model: Semantic Source Path Boundaries

No report fields or entities are added. Existing `CSharpSemanticDeclaration`, `CSharpResolvedCall`, and `CSharpUnresolvedCall` records continue to carry source locations. Their path invariants are tightened:

- `Project` and source `Path` identify files within the scanned repository using `/` separators.
- `TargetPath` and `TargetLine` are populated only when the resolved source declaration is inside the scanned repository; both are null for external or metadata-only targets.
- Declaration and invocation counts cover only in-repository syntax trees.
