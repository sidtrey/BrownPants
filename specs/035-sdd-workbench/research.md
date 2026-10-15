# Research: Local SDD Review Workbench

## Repository evidence

- BrownPants currently emits Markdown/JSON reports and has Spec Kit/OpenSpec inventory, but no interactive browser UI.
- Spec Kit features store user stories, acceptance scenarios, tasks, acceptance evidence, and verification in Markdown/JSON under `specs/<feature>/`.
- The active Spec Kit feature pointer is stored in `.specify/feature.json` as a repository-relative `feature_directory`.
- Existing Azure DevOps trace and PR preview are CLI flows; this workbench is a local review handoff, not a replacement for Azure Boards or Git.

## Decisions

- Generate a static `.html` file that opens from disk; do not add an HTTP listener, desktop UI runtime, or hosted dependency.
- Support one Spec Kit feature per report, selecting the active feature by default and allowing explicit repository-relative selection.
- Parse only known summary patterns; show the complete Markdown source in expandable text panels so parsing limitations do not hide requirements.
- Store reviewer feedback in a user-downloaded JSON file, never in the source repository automatically.
- Use a small hard limit of 2 MiB per input artifact and 8 MiB total report input to bound memory and output size.

## Alternatives considered

- A local web server was rejected for v1 because static files avoid listener, authentication, and browser-origin setup.
- An embedded browser application or GUI framework was rejected because it adds platform/runtime packaging.
- A full Markdown renderer or frontend package was rejected to keep supply-chain and offline behavior simple; raw Markdown remains available as text.
