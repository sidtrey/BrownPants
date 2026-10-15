# Research: NHibernate Awareness

- NHibernate's official reference documents `Configuration` building an `ISessionFactory` after mappings are added, including mappings from `.hbm.xml` resources and `hibernate.cfg.xml`. These are useful explicit configuration signals, but their presence does not prove the application loads them. Source: [SessionFactory configuration](https://nhibernate.info/doc/nhibernate-reference/session-configuration.html).
- The official concurrency guidance describes `ISessionFactory` as an expensive shared thread-safe object and `ISession` as an inexpensive non-thread-safe unit of work. BrownPants can locate session/factory APIs; it cannot judge actual lifetime from a name match. Source: [Transactions and concurrency](https://nhibernate.info/doc/nhibernate-reference/transactions.html).
- HBM mappings are conventionally named `*.hbm.xml`, but XML namespace/root markers are stronger evidence than filename alone. Source: [Basic O/R Mapping](https://nhibernate.info/doc/nhibernate-reference/mapping.html).
- Keep matching within existing Roslyn syntax and safe XML parsing. Avoid new packages, NuGet restore, semantic project load, and network access during analysis.

## Decisions

1. Report source and direct metadata candidates; do not claim transitive package resolution.
2. Require NHibernate namespace/import or explicit fully qualified type/API evidence for ambiguous C#/VB method names.
3. Treat file names as supporting evidence; `hibernate-mapping` and `hibernate-configuration` XML markers strengthen classification.
4. Preserve existing architecture signal schema, renderer, deterministic ordering, and sample limits.
