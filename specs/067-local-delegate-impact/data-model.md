# Data Model: Local Delegate Impact

## Delegate Binding

One source local and its single compiler-bound delegate initializer.

- **Project**: repository-relative project path.
- **Path**: repository-relative syntax-tree path.
- **Local identity**: Roslyn local symbol identity scoped to its callable.
- **Initializer**: source location and operation kind.
- **Target callable**: canonical method or anonymous-function identity selected by Roslyn.
- **Target location**: repository-relative source path and line when the callable is source-backed; otherwise null.
- **Use sites**: invocation locations that use the local as the direct delegate receiver.
- **Resolution state**: resolved only for a single safe local binding; otherwise unresolved with a reason.

## Delegate Relationship

An additive managed call-graph edge from the bound target callable to the enclosing callable at each invocation site.

- **RelationshipKind**: `delegate`.
- **Expression**: the source invocation expression, such as `callback()` or `callback.Invoke()`.
- **Source location**: repository-relative invocation path, one-based line and column.
- **Target identity**: the same canonical callable identity used by method calls and impact graph joins.
- **Counts**: delegate relationship totals are separate; existing resolved invocation and constructor counts do not change.

## Unresolved Delegate Target

A source-located local delegate invocation whose target is not safe to infer.

- **Reason**: stable category such as reassigned, captured, escaped, unsupported binding, or ambiguous use.
- **Sample**: project, path, line, expression, reason, and bounded compiler candidate targets when available.
- **Completeness**: any unresolved delegate target makes managed impact incomplete; exact count and omitted sample count remain visible.

These records describe static source relationships. They do not prove the delegate is invoked at runtime.
