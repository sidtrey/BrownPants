# Verification: Nested Callable Identities

## Regression Evidence

- The C# semantic fixture covers two independent lambdas, repeated calls in one lambda, nested lambdas, an anonymous method, and a local function. It asserts relative source anchors, shared identity within one lambda, distinct identity across lambdas, and matching local-function declaration, caller, and target identities.
- The VB fixture resolves a call inside a lambda and asserts the named enclosing method, relative source anchor, and absence of an empty method-name segment.
- The impact fixture finds two distinct callers for calls to `Changed.Save` and verifies repeated JSON, Markdown, and graph output is stable.
- Focused Release test run: 3 passed, 0 failed, 0 skipped.

## Brownfield Bug Lab

Command:

```powershell
dotnet run --project src/BrownPants.Cli -c Release --no-build -- impact examples/brownfield-bug-lab --changed managed/Workflows/OrderImportWorkflow.cs --format json --limit 1000
```

Observed managed impact is complete with 10 edges. Three call records use the callback identity `global::LegacyOrderDesk.Workflows.OrderBatchFactory.Register(global::LegacyOrderDesk.Workflows.WorkScope)::<lambda@managed/Workflows/OrderImportWorkflow.cs:26:59>`, including calls to `OrderBatchFactory.Create` and `WorkScope.Get`. The same changed workflow produces three native lexical candidates at native-only depths 1, 2, and 3: C++/CLI bridge, qualified native call, and MFC dispatch. Candidate links remain lexical evidence and do not prove compiler binding or runtime execution.

## Release Gates

- `dotnet build BrownPants.sln -c Release`: passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build`: 288 passed, 0 failed, 0 skipped.
- `inspect . --semantic --format json --limit 10`: 5 of 5 projects loaded; 10,764 invocation expressions, 10,738 resolved, 0 unresolved; 1,132 constructors, all 1,132 resolved; project coverage complete; 0 compiler errors and 0 workspace issues.
- Acceptance criteria digest verified: `0e2d31146476965693b35b3474845639be463efb4b25074e156b381bb9f94f4c`.
- Evidence ledger validator: passed for all 6 tasks and 10 criteria; final criteria digest check passed after implementation.

## Limits

Callable identities describe compiler-reported source scope and location. They do not establish that delegates execute, the order in which callbacks run, or that native candidates compile or dispatch at runtime. External source anchors are redacted by the formatter.
