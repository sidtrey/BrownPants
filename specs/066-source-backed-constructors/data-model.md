# Data Model: Source-Backed Constructor Declarations

No new report entity is required. The existing `CSharpSemanticDeclaration` represents a source declaration with project path, repository-relative source path, one-based line, and canonical symbol identity.

For explicit constructors, the current constructor syntax node remains the source anchor. For primary constructors, the containing type declaration is the anchor. For an implicit constructor, the containing type declaration anchors the generated constructor only when Roslyn binds an in-repository source location to that type. Constructor call edges continue to use `RelationshipKind = constructor` and the same canonical symbol identity.

Declarations are unique by project, path, line, and symbol. Metadata-only constructors have no repository declaration entry. Existing constructor syntax and resolved/unresolved counts remain separate from method invocation counts.
