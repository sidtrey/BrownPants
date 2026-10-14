# Research: Report Python Factory and DI Signals

## Decisions

- Treat Python comments and string literals as lexical content to mask before candidate matching. Python's language reference defines `#` comments and single/triple-quoted strings: [lexical analysis](https://docs.python.org/3/reference/lexical_analysis.html).
- Recognize FastAPI `Depends` as dependency declaration evidence, not a resolved graph: [FastAPI dependency documentation](https://fastapi.tiangolo.com/tutorial/dependencies/).
- Recognize Dependency Injector `providers.Factory` and `providers.Singleton` as provider declarations: [Factory provider](https://python-dependency-injector.ets-labs.org/providers/factory.html), [Singleton provider](https://python-dependency-injector.ets-labs.org/providers/singleton.html).
- Keep analysis lexical, bounded, and dependency-free.

## Rationale

These conventions provide useful construction and wiring leads in Python without requiring framework loading or pretending to resolve runtime relationships.

## Alternatives Considered

- Invoke Python's `ast` module: rejected because default repository inspection must not require an external interpreter.
- Report all calls to `create()` or classes named `Factory` as facts: rejected; keep them explicitly labeled candidates and filter comments/strings.
