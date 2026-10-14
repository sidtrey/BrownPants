# BrownPants Constitution

## Core Principles

### I. Evidence First

Reports MUST distinguish observed facts from heuristics and candidate signals. Every
source-level claim MUST identify its evidence and limits. BrownPants MUST NOT present
syntax matches as resolved relationships or inventory markers as proof of buildability,
runtime behavior, or active dependencies.

### II. Broad, Engine-Neutral Core

The core inventory MUST serve mixed repositories and remain independent of any single SDD
engine. It MUST recognize supported languages and project systems through explicit evidence
levels. Engine-specific lifecycle behavior belongs in adapters. Managed/native analysis MUST
treat both sides of the boundary as relevant evidence.

### III. Deterministic and Reviewable Output

The same repository contents and options MUST produce the same report. Inventory MUST use
stable ordering, bounded samples, repository-relative paths, and explicit evidence limits.
Markdown MUST be readable by people; JSON MUST remain useful to tools and other SDD engines.

### IV. Safe Inspection by Default

Default inspection MUST read repository content without executing project code, invoking
build tools, accessing the network, or modifying the inspected repository. Features that load
MSBuild projects or evaluate imports MUST be opt-in and MUST explain that project targets may
run. Repository configuration MUST remain declarative, bounded, and unable to direct reads
outside the scanned inventory.

### V. Specified and Verified Changes

Behavior changes MUST begin with a reviewed specification and implementation plan in the
repository's Spec Kit workflow. Tasks MUST identify concrete files and observable results.
Changed behavior MUST have fixture-based tests. A change is ready when the relevant build,
test, adapter, and package checks pass and documentation states material evidence limits.

## Technical Constraints

- The CLI targets .NET 10. Roslyn provides syntax analysis for C# and Visual Basic,
  including source from legacy .NET Framework projects.
- Native MFC, COM, and C++/CLI findings MUST state whether they are lexical candidates,
  syntax findings, or semantically resolved evidence.
- The core MUST support repositories that do not use .NET and MUST NOT require an SDD
  engine, an LLM, network access, or project evaluation for default inspection.
- Semantic project loading remains an explicit option because MSBuild imports and targets
  can execute code.

## Development and Quality Gates

- Use four spaces, nullable reference types, PascalCase public APIs, and camelCase locals
  and parameters.
- Add or update small temporary-repository fixtures for scanner behavior, including mixed
  ecosystems, ignored directories, invalid configuration, and path-safety cases where
  relevant.
- Verify changes in Markdown and JSON where both formats expose the behavior.
- Run `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, and the Spec Kit and
  OpenSpec adapter checks for changes that affect those integrations.
- Run a packed-tool smoke check when packaging or changing the command-line contract.
- Review generated reports before sharing them because paths and project structure may be
  sensitive.

## Governance

This constitution governs product requirements and design decisions. [AGENTS.md](../../AGENTS.md)
provides operational contribution guidance and MUST be read with the constitution. If a
proposal conflicts with a principle, revise the proposal or amend the constitution through a
separate Spec Kit constitution update. Implementation plans and reviews MUST record how the
principles apply.

Amendments require a documented rationale and a semantic version update. Increment MAJOR
when a principle is removed or materially redefined; MINOR when a principle or binding
requirement is added or materially expanded; PATCH for clarifications that do not change
commitments. Review principle alignment during planning and before merge.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): Original adoption date not recorded | **Last Amended**: 2026-10-14
