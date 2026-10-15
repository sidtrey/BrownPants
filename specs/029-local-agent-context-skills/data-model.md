# Data Model: Local Agent Context Skills

## Skill Payload

- Canonical source: one bundled `SKILL.md` with a stable skill name and host-neutral evidence instructions.
- Installed destinations: one path per selected host, relative to an existing project root.
- Content behavior: exact byte copy; an identical destination is a no-op; a different destination is preserved and reported as a conflict.

## Install Request

- Project root: existing directory selected by the maintainer.
- Host selection: `codex`, `claude`, or `both`.
- Result: installed or already-current relative path per destination, or a failure that names the invalid root, path escape, or conflicting destination.

The installer stores no state, credentials, or machine-specific configuration.
