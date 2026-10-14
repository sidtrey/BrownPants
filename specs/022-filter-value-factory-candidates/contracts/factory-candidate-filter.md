# Factory Method Candidate Filter Contract

## Input

Readable C# and VB.NET files in a repository scan.

## Observable behavior

- A method with a `Create`, `Build`, or `Resolve` prefix and a string return type is not reported as a `Factory method` based on its name alone.
- A corresponding method returning an object type remains a `Factory method` candidate.
- C# and VB.NET findings retain their source path, line, evidence kind, and evidence excerpt.
- Markdown and JSON expose the same resulting candidate set.
- No output schema fields are added or removed.

## Limits

The finding is a syntax-based candidate, not proof of runtime construction or dependency relationships. Other return types retain existing behavior.