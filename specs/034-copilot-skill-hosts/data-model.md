# Data Model: Copilot GUI Skill Hosts

This feature introduces no persisted runtime data model. The installer uses one immutable bundled skill source and a selected set of target definitions.

Each target definition contains:

- Host label for installation output.
- Repository-relative skill directory.
- Resolved destination directory beneath the chosen project root.
- Destination state: missing or identical; a differing file is a preflight conflict.

The target set is fully validated before any write begins.
