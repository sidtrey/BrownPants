# Research: Indirect Placeholder Accessors

## Decision

Recognize one-hop invocation wrappers whose target is a same-type method with a body consisting only of a direct `NotImplementedException` throw. Include expression-bodied methods and property getters. Preserve the current analyzer's source byte cap, sample limit, stable ordering, and candidate-only wording.

## Evidence

- BrownPants live parser-backed inventory of FullMetalFile reports `PostgreSqlUnitOfWork.CreateNotImplemented` but not the five expression-bodied repository accessors that call it.
- The five accessors are visible in `FullMetalFile.Infrastructure/Persistence/PostgreSQL/PostgreSqlUnitOfWork.cs`; each delegates to the same helper.
- `CSharpImplementationPlaceholderAnalyzer` currently visits only `MethodDeclarationSyntax` and classifies a method's own direct body.

## Alternatives considered

- Semantic call graph resolution was rejected for this candidate: the one-hop same-type syntax shape is enough to provide navigation evidence and keeps default inspection independent of MSBuild evaluation.
- Multi-hop and inherited delegation are excluded to avoid implying resolved dispatch or broad call-chain coverage.

## Constitution alignment

The change is deterministic, uses Roslyn syntax, preserves non-.NET repository support, requires no network or MSBuild evaluation, and must state that findings are heuristic candidates.
