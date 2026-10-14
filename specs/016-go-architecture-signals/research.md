# Research: Go Factory and Dependency-Injection Signals

## Decisions

- Treat `NewType` functions as constructor/factory candidates. Effective Go uses `NewFile` and `NewVector` examples for functions that initialize values: [Effective Go](https://go.dev/doc/effective_go).
- Recognize Fx `Provide` and `Invoke` calls as provider and application-entry wiring; the [Fx package documentation](https://pkg.go.dev/go.uber.org/fx) describes provided dependencies and invoked functions.
- Recognize Dig container `Provide` and `Invoke` calls as wiring candidates; see the [Dig package API](https://pkg.go.dev/go.uber.org/dig).
- Keep names and framework calls lexical. BrownPants will not resolve imports, aliases, constructors' return types, or the resulting graph.
- Mask Go raw backtick strings along with ordinary strings, rune literals, and comments so pattern-shaped data does not become source evidence.

## Rationale

Effective Go documents common constructor function names, while Fx and Dig expose recognizable provider/invocation APIs. These give repository navigation leads without installing Go tools or evaluating application code.

## Alternatives Considered

- Run `go list`, `go test`, or a Go parser/compiler: rejected because default scanning should remain dependency-free and safe for unknown repositories.
- Treat every `New...` function or `Provide` call as a confirmed factory/binding: rejected; matches remain candidates, and naming may be unrelated.
- Support custom import aliases and arbitrary frameworks: deferred because lexical matching cannot safely infer arbitrary package identities.
