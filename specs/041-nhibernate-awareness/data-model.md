# Data Model: NHibernate Awareness

The feature reuses `ArchitecturePatternSignal`:

- `Category`: dependency, configuration, mapping, session factory, session, query, transaction, or DI registration.
- `EvidenceKind`: `Candidate`.
- `Language`: C#, VB.NET, XML, or project metadata.
- `Path` and `Line`: repository-relative source location; metadata-level package evidence may use line 0 if the existing XML helper cannot supply a stable line.
- `Evidence`: bounded source line or metadata identifier under the existing report behavior.

Signals are observations. They do not describe resolved package graphs, loaded mappings, database behavior, session ownership, or thread safety.
