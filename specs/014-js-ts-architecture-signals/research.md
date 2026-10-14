# Research: Report JavaScript and TypeScript Factory and DI Signals

## Decisions

- Treat comments and string/template literal contents as non-code when matching. JavaScript lexical grammar defines line/block comments and quoted/template literals: [MDN lexical grammar](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Lexical_grammar).
- Recognize Angular provider `useFactory` and `inject(...)` as DI cues: [Angular provider definitions](https://angular.dev/guide/di/defining-dependency-providers), [Angular `inject` API](https://angular.dev/api/core/inject).
- Recognize Inversify `@injectable`, `@inject`, `container.bind`, and `container.get` as DI cues: [Inversify getting started](https://inversify.io/docs/next/introduction/getting-started/), [Inversify binding](https://inversify.io/docs/6.x/fundamentals/binding/).
- Keep findings lexical, bounded, and dependency-free.

## Rationale

Angular and Inversify document concrete, source-visible provider and injection conventions. These patterns provide reviewable navigation evidence across JavaScript and TypeScript without loading frameworks or resolving a graph.

## Alternatives Considered

- Invoke a JavaScript runtime or TypeScript compiler: rejected because default inspection must remain dependency-free and must not execute project code.
- Treat DI annotations as proof of active framework wiring: rejected; findings remain candidates until source and configuration are verified.
