# Research: Report the Active SDD Work Item

## Decisions

### Selection evidence

- Spec Kit's `.specify/feature.json` contains the local `feature_directory` pointer. The repository's current pointer names `specs/008-test-brownpants-spec`; the pointer itself is ignored as per-checkout state.
- OpenSpec has no single active pointer in the supported inventory contract. A unique non-archived first-level change directory is sufficient evidence; zero or multiple candidates remain unset.
- Custom SDD declarations currently describe artifact, specification, and change roots but do not define active selection. Keep their active work item unset rather than guessing.

### Safety and compatibility

- Normalize either slash style in the Spec Kit pointer, then reject absolute paths, traversal segments, empty segments, drive/URI syntax, wildcards, control characters, and surrounding whitespace.
- Read only the already discovered `.specify/feature.json`; bound its size and reject reparse points. Confirm the normalized pointer is a directory prefix of at least one scanned repository-relative file before reporting it.
- Add nullable selection fields to `SddWorkflowInventory`. This is an additive JSON schema version 1 change; existing properties and counts remain unchanged.
- Attach a stable evidence label to each selected path so downstream agents can distinguish a user-selected Spec Kit pointer from an inferred unique OpenSpec candidate.

## Alternatives Considered

- Treat the first sorted feature or change as active: rejected because ordering is not selection evidence.
- Select any Spec Kit feature when `feature.json` is missing or stale: rejected because it can direct work to the wrong specification.
- Guess active custom workflow items from counts: rejected because declarations do not encode selection intent.
- Read the pointer target from disk directly: rejected because matching against the scanner's existing file inventory enforces the repository boundary and avoids following paths from configuration.
