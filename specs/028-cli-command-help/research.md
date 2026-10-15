# Research: CLI Command Help

## Current Behavior

- `Program.Run` recognizes global `--help`, `-h`, and `help` only in the first argument.
- Global help lists command syntax but does not describe command purpose or option effects.
- Command parsers treat trailing `--help` as an unknown option. A direct probe of `inspect --help` returned `Unknown option '--help'.`
- The README and inspect report already distinguish syntax-only invocation evidence from semantic target resolution and warn about MSBuild evaluation, but this guidance is not available in command help.

## Decision

Route help requests before command parsers. Provide one global help renderer and deterministic command renderers for all commands, including `frame create` and `frame validate`. Keep command parsing and execution unchanged when help is absent. Add tests through `Program.Run`, using an invalid repository path to prove help does not start inspection.

## Alternatives Considered

- **Only add `inspect --help`**: rejected because other commands have the same discoverability problem.
- **Point users to the README only**: rejected because help should work offline and directly at the point of use.
- **Parse a full command-line library**: rejected as unnecessary for this bounded help behavior and current small CLI.
