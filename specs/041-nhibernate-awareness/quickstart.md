# Quickstart: NHibernate Awareness

1. Run `dotnet run --project src/BrownPants.Cli -- inspect <repository> --format markdown --limit 100`.
2. Review `Architecture pattern signals` for NHibernate candidate categories and source locations.
3. Use `--format json` to consume stable fields in automation.
4. Verify package references, mapping inclusion, DI lifetime, and actual session ownership in the application. BrownPants does not resolve runtime ORM behavior.

Fixture coverage should include C# and VB API usage, project/package references, `.hbm.xml`, `hibernate.cfg.xml`, comments and strings, unrelated types named `Session` or `Transaction`, malformed XML, deterministic limits, and unchanged source files.
