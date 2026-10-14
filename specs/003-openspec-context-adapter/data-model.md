# Data Model: OpenSpec BrownPants Context Adapter

## SDD workflow inventory

`SddWorkflowInventory` represents a supported workflow without adding engine-specific fields to the top-level repository model:

- **Name**: `Spec Kit` or `OpenSpec`.
- **IsInitialized**: workflow files are present in the conventional root directory.
- **SpecificationDirectoryCount**: first-level feature/capability directories containing files.
- **ActiveChangeDirectoryCount**: active change directories, or null when the workflow does not track changes separately.
- **ArtifactFileCount**: total discovered files for that workflow.
- **ArtifactPaths**: deterministic paths capped by the inspect sample limit.

OpenSpec archive directories are not included in `ActiveChangeDirectoryCount`. Omitted artifact paths are reported in Markdown; JSON carries the total and sampled paths separately.

## Installer inputs

- **Project root**: current directory by default; must contain `openspec/` unless an explicit project root is given.
- **Skill source**: the checked-in `SKILL.md` under `integrations/openspec/skills/openspec-brownpants-context/`.
- **Skill destination**: `<project-root>/.agents/skills/openspec-brownpants-context/SKILL.md`.

## Installer outcomes

- **Installed**: destination was absent and copied from source.
- **Already current**: destination content matches source; no write occurs.
- **Conflict**: destination differs; preserve it and exit nonzero with manual-reconciliation guidance.

## Context evidence

The skill consumes the existing Markdown report and references paths relative to the OpenSpec project: `openspec/specs/`, `openspec/changes/<change>/proposal.md`, `design.md`, `tasks.md`, and delta specs. These paths are optional observations, not required fields.
