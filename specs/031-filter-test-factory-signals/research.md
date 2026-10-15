# Research: Filter Test Methods from Factory Signals

## Decision: Use syntax-only method attribute names

The existing analyzer parses C# and VB source with Roslyn without loading project references. The feature will inspect method attribute lists and normalize the syntactic attribute name (qualified name's final segment, optional `Attribute` suffix, case-insensitive comparison). It will recognize only the explicit framework marker names in the specification.

**Rationale**: This preserves deterministic, offline inspection and works even when legacy projects cannot be evaluated or test framework packages are unavailable. The result remains a heuristic candidate signal.

**Alternatives considered**: Semantic attribute symbol resolution would require a compilation and references not available to the current syntax-only pass; broad test-directory suppression would hide legitimate production factories and fail for colocated tests.

## Decision: Anchor method findings at the method statement/declaration

Roslyn declaration nodes may include leading attribute trivia in their full source span. Use the syntax node's declaration token/location and normalized declaration text for method-level factory findings, while leaving other signal locations unchanged.

**Rationale**: Current self-inspection shows `[Fact]` as evidence for test methods, and users need the actual signature and source line.

**Alternatives considered**: Changing location behavior globally could alter unrelated C#, VB, and native findings; the implementation should narrowly adjust method findings.

## Decision: Keep scope within C# and VB factory-method classification

Other language analyzers and factory invocations are unchanged. A test method may still emit a separate DI or interop signal.

**Rationale**: This matches the specification and avoids broadening the feature into framework discovery for every supported language.
