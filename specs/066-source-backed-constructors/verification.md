# Verification: Source-Backed Constructor Declarations

## Regression Evidence

Before the analyzer change, C# construction resolved to constructor symbols but declarations for the implicit and primary constructors were absent; impact from the two type files was incomplete. The VB fixture also resolved its implicit constructor call but had no matching declaration. These failures reproduced the missing source anchor in each supported case.

After the change:

- C# implicit parameterless and primary constructors each have one declaration matching the resolved constructor target, path, and line.
- A Visual Basic implicit constructor is source-anchored when Roslyn exposes its source symbol.
- Impact from both C# type files is complete and returns the correct construction caller at depth 1 with `relationshipKind: constructor`.
- A constructor target in an external source tree keeps null repository target path/line, and serialized semantic evidence does not expose the external absolute path.
- Existing constructor overload, target-typed construction, chaining, VB construction, metadata-target privacy, and method-impact tests pass.
- Focused source/impact/privacy regression run: 4 passed, 0 failed, 0 skipped.

## Brownfield Bug Lab

Command:

```powershell
dotnet run --project src/BrownPants.Cli -c Release --no-build -- impact examples/brownfield-bug-lab --changed managed/Workflows/OrderImportWorkflow.cs --format json --limit 1000
```

Observed impact is complete with 11 managed relationships, including four constructor edges. These include `OrderBatchFactory.Create` constructing `OrderImportWorkflow<T>`, the source-anchored DI lambda constructing `OrderBatchFactory`, and two test callers constructing the workflow. The report still includes three native lexical candidates at depths 1, 2, and 3 for the C++/CLI bridge, qualified native call, and MFC dispatch. Native relationships remain candidates, not compiler-bound calls.

## BrownPants Semantic Self-Inspection

`inspect . --semantic --format json --limit 10` loaded all 5 of 5 discovered projects with complete project coverage, 0 workspace issues, and 0 compiler errors. It reported 10,839 invocation syntax nodes (26 skipped `nameof` expressions, 10,813 analyzed and resolved, 0 unresolved) and 1,137 constructor syntax nodes (1,137 resolved, 0 unresolved). Implicit constructor declarations are now included in the semantic declaration count.

## Gates

- `dotnet build BrownPants.sln -c Release`: passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build`: 291 passed, 0 failed, 0 skipped.
- Acceptance criteria digest: `60cfb0e80a9e8b1bb92a3ef62d3450112a17c45fd67d5fd85fffd0efccd3b3a8`.
- Evidence ledger validation: passed for all 6 tasks and 8 criteria.
- `git diff --check`: passed.

## Limits

Constructor declarations identify compiler symbols at source locations. A changed type file can affect generated constructor behavior, but the report does not prove that a particular construction executes. Metadata-only constructors remain without repository source paths; dependency injection, reflection, and native activation are not inferred.
