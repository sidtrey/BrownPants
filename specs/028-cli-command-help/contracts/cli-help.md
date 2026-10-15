# Contract: CLI Help

- Global help is available through `--help`, `-h`, or `help`.
- A top-level command accepts trailing `--help` or `-h`; frame create and validate have separate help topics.
- Help requests return success and are handled before repository analysis or file creation.
- Inspect help explicitly distinguishes syntax-only invocation inventory from semantic target resolution and explains possible MSBuild imported-target evaluation.
- Unknown help topics return a usage error and direct the user to global help.
- Help text is deterministic and available offline.
