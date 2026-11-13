# Data Model: C# Consumption of C++/CLI Libraries

## Managed Project Reference

- `projectPath`: repository-relative C# project path.
- `referenceKind`: project reference or assembly reference.
- `identity`: static project/assembly identity when present.
- `hintPath`: normalized repository-relative candidate path for a legacy assembly reference; null when absent or unsafe.
- `candidateNativeProjectPaths`: bounded set of discovered CLR-enabled native project paths matching the available reference evidence.
- `status`: unique candidate, ambiguous, unresolved, or unsupported.

## Managed Consumer Call

- `sourcePath`, `sourceLine`, `sourceColumn`: C# call-site identity.
- `caller`: containing C# method when available.
- `receiverType`, `methodName`: syntax/local-type evidence used for matching.
- `projectPath`: containing managed project when determinable.
- `referenceStatus`: status of the project-to-native reference association.

## Managed-to-C++/CLI Relation

- `kind`: `csharp-cppcli-call`.
- `status`: `candidate`, `ambiguous`, `unresolved`, or `unsupported`.
- `sourcePath`, `sourceLine`, `sourceSymbol`: managed consumer call.
- `targetSymbol`: candidate wrapper type and method identity.
- `targetPath`, `targetLine`: populated only for one unique source declaration/definition.
- `candidateTargets`, `candidateTargetCount`: every retained target location and total before sampling.
- `matchReasons`: source/reference facts that supported the association.

## C#-First Impact Path

An impact path begins with a changed C# consumer file and follows a `csharp-cppcli-call` candidate to the wrapper symbol, then follows supported qualified native-call relations. It remains separate from compiler-resolved managed impact and from reverse C++/CLI-to-managed paths. Completeness is false when a relevant reference/source index or relation sample is truncated or unresolved.
