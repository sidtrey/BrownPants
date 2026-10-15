# Research: OpenSpec Review Workbench

## Artifact contract

OpenSpec's current change format groups artifacts in `openspec/changes/<name>/`: `proposal.md`, optional `design.md`, `tasks.md`, and capability delta specs under `specs/<capability-path>/spec.md`. Its spec conventions use `### Requirement:` headings followed by `#### Scenario:` headings. Sources: [OpenSpec concepts](https://github.com/Fission-AI/OpenSpec/blob/main/docs/concepts.md), [OpenSpec conventions](https://github.com/Fission-AI/OpenSpec/blob/main/openspec/specs/openspec-conventions/spec.md).

## Selection

The existing repository inventory recognizes a unique non-archived first-level OpenSpec change as active. The workbench can use the same convention but must still validate the selected path and artifact reads itself; inventory text is not a security boundary. Explicit `--feature` selection avoids guessing when multiple changes exist.

## Rendering and feedback

Reuse the existing static HTML renderer and local note download. OpenSpec does not use the Spec Kit `AC-###`/`SC-###` vocabulary or acceptance hash ledger, so label parsed requirement/scenario references with their source path and leave unrecognized text in expandable original artifacts. Never call a task checked off evidence of implementation or acceptance.

## Constraints

Keep the existing .NET-only implementation, bounded strict UTF-8 artifact reading, repository containment and reparse-point checks. Do not invoke OpenSpec CLI, parse its YAML metadata, validate or archive a change, or evaluate project code.
