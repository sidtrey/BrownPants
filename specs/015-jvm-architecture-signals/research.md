# Research: JVM Factory and Dependency-Injection Signals

## Decisions

- Recognize Spring `@Configuration` and `@Bean` as bean-definition/factory cues. Spring documents configuration classes and bean methods in its [Java configuration guide](https://docs.spring.io/spring-framework/reference/core/beans/java/basic-concepts.html).
- Recognize `@Inject` as an injection cue; Jakarta Inject describes it for constructors, methods, and fields. See the [Jakarta Inject API](https://javadoc.io/static/jakarta.platform/jakarta.jakartaee-api/9.1.0/jakarta/inject/package-summary.html).
- Recognize Dagger `@Provides` and `@Binds` as binding cues; see the [Dagger developer guide](https://dagger.dev/dev-guide/basic-usage).
- Recognize Guice `Module` binding declarations and `Guice.createInjector` as wiring cues; see the [Guice Module API](https://google.github.io/guice/api-docs/latest/javadoc/com/google/inject/Module.html) and [Guice AbstractModule API](https://google.github.io/guice/api-docs/latest/javadoc/com/google/inject/AbstractModule.html).
- Keep these framework annotations as lexical candidates; annotation presence alone does not establish a configured or valid object graph.
- Support Java `.java`, Kotlin `.kt`, and Kotlin script `.kts` source using the repository's existing extension inventory.

## Rationale

These conventions represent documented factory and binding declarations across widely used JVM dependency-injection systems. Lexical recognition adds navigation evidence without downloading dependencies, building the project, or claiming semantic resolution.

## Alternatives Considered

- Invoke javac/kotlinc or a build system: rejected because default inspection must not evaluate project code or require toolchains.
- Claim all annotations are active DI: rejected because frameworks and imports are not resolved.
- Add additional JVM languages in this feature: deferred; Java and Kotlin already have explicit inventory coverage and directly share the targeted JVM frameworks.
