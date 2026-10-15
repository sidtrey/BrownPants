# Contract: Architecture Pattern Evidence

The `inspect` command's JSON and Markdown architecture-pattern outputs retain their current shape. Each candidate contains a category, evidence kind, language, repository-relative path, one-based line, and compact evidence text.

For C# and VB factory-method findings:

- Methods with syntactic attributes named `Fact`, `Theory`, `Test`, `TestCase`, `TestCaseSource`, `TestMethod`, or `DataTestMethod` (with optional `Attribute` suffix and qualification) are excluded from the `Factory method` category.
- The exclusion applies only to factory-method classification; other independently detected patterns are unaffected.
- Emitted method candidates point to the method declaration/statement and show its signature, even when attributes or comments precede it.
- `EvidenceKind` remains `Candidate`; matching is syntax-based and does not resolve attribute types.

No CLI switches, JSON fields, or other-language behavior are added.
