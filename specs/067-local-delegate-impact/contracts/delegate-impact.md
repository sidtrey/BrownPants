# Contract: Semantic Delegate Impact

## Resolved edge

Semantic reports add a resolved relationship with `RelationshipKind: "delegate"`. It carries the existing project, source path, one-based line, expression, enclosing caller, target signature, optional in-repository target path/line, canonical target identity, and source column fields used by managed calls. Impact output can traverse the target to the invocation caller and continues to label the edge as compiler-derived static evidence.

Delegate target edges do not increment invocation syntax, resolved invocation, or constructor counts. Existing method and constructor report fields retain their meanings. Existing report schema versions remain unchanged; new properties are additive.

## Unresolved edge

Unsafe local-delegate flows are reported separately using a bounded count and source-located sample with an explicit reason. They are not emitted as resolved relationships. Any unresolved delegate target contributes an issue and marks managed impact incomplete. External method targets may have no repository target location.

## Safety boundary

The analyzer supports only a single local initializer and direct local invocation uses within the same callable. It does not infer field, parameter, event, returned, escaped, captured, reassigned, reflection-driven, container-managed, or runtime delegate targets. Markdown and JSON use repository-relative paths; external source paths remain omitted or redacted according to existing semantic report conventions.
