# Feature Specification: NHibernate Awareness

**Feature Branch**: `041-nhibernate-awareness`

**Created**: 2026-10-15

**Status**: Draft

**Input**: FIFO idea 17: recognize NHibernate packages, configuration, mappings, session factories, dependency-injection registrations, and common query/transaction usage in .NET brownfield repositories, with source-located evidence and clear limits.

## User Scenarios & Testing

### User Story 1 - Identify NHibernate usage (Priority: P1)

A maintainer entering a .NET system wants to know whether and how it uses NHibernate before changing persistence code.

**Why this priority**: ORM conventions, session lifetime, and mappings can shape change risk across legacy systems.

**Independent Test**: Analyze temporary C# and VB repositories containing package metadata, mappings, session setup, queries and transactions, alongside unrelated lookalike names.

**Acceptance Scenarios**:

1. **AC-001** Given NHibernate package references or recognizable mapping/configuration files, when `inspect` runs, then its architecture candidates identify the evidence category and relative path.
2. **AC-002** Given C# or VB source using recognizable NHibernate session, factory, query, or transaction APIs, when `inspect` runs, then each bounded candidate is source-located and labeled as candidate evidence.
3. **AC-003** Given ordinary types or text that only contain similar names, when analysis runs, then comments, strings, unrelated `Session` types, and generic transaction APIs are not reported as confirmed NHibernate usage.

### User Story 2 - Review scope and evidence limits (Priority: P1)

A developer or QA reviewer needs deterministic evidence that works without loading the target application or resolving dependencies.

**Independent Test**: Repeat an offline inspection of a mixed-language temporary repository and verify stable bounded Markdown/JSON findings, no source writes, and explicit limits.

**Acceptance Scenarios**:

1. **AC-004** Given identical repository files, when Markdown and JSON reports are generated, then candidate identities and locations are deterministic and findings respect the sample limit.
2. **AC-005** Given a source-only pattern match, when it is reported, then the report says it does not prove runtime ORM configuration, mapping activation, session lifetime, or database behavior.

## Edge Cases

- NHibernate may be referenced only by transitive packages or generated code.
- XML mappings may use embedded resources or custom naming; not every XML file with mapping-like elements is NHibernate.
- `ISession`, `SessionFactory`, `Query`, and `Transaction` names may belong to unrelated libraries.
- Fluent NHibernate APIs and classic `.hbm.xml` mappings differ from modern mapping-by-code.
- Conditional project references and generated files are not evaluated.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST recognize direct NHibernate package references in common .NET project/package metadata.
- **FR-002**: BrownPants MUST identify source-located candidates for NHibernate session factory, session, query, transaction, and dependency-injection registration patterns in C# and VB.
- **FR-003**: BrownPants MUST identify mapping/configuration candidates only from explicit NHibernate markers or recognized mapping file conventions.
- **FR-004**: Findings MUST use candidate evidence with stable category, language, path, and line; bounded output remains deterministic.
- **FR-005**: Analysis MUST NOT evaluate MSBuild, execute source, access network, connect to databases, or modify the inspected repository.
- **FR-006**: Reports MUST state that syntax/configuration candidates do not prove runtime package resolution, mapping activation, session scope, or database behavior.
- **FR-007**: Comments and string literals MUST NOT create source API candidates; unrelated generic names MUST not be called NHibernate without namespace/type evidence.

### Key Entities

- **NHibernate candidate**: A source- or metadata-located signal categorized as dependency, configuration/mapping, session/factory, query, transaction, or DI registration.
- **Evidence limit**: A stated constraint describing what the source-level signal cannot establish about runtime behavior.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Fixtures identify all expected package, mapping, and source API examples in C# and VB while excluding lookalike comments, strings, and ordinary types.
- **SC-002**: Repeated Markdown/JSON inspections produce stable finding identities and locations and respect the requested result limit.
- **SC-003**: Inspection completes without evaluating projects, network access, database connections, source execution, or repository writes.
- **SC-004**: Every candidate is clearly labeled and states that it is not proof of runtime ORM behavior.

## Assumptions

- V1 extends BrownPants' existing architecture-pattern candidate report; it does not create a new workflow or ORM migration tool.
- Direct package/configuration metadata and syntax-visible APIs are the supported evidence. Transitive dependency resolution and runtime configuration are outside scope.
