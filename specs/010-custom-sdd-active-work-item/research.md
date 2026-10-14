# Research: Custom SDD Active Work Item

## Decisions

- Extend the existing optional `.brownpants/sdd-workflows.json` schema version 1 workflow object with `activeWorkItemPath`.
- Treat this value as explicit maintainer supplied selection intent. Normalize slash styles, validate it using repository-relative path rules, and require it to be a descendant of one declared `artifactRoots` entry.
- Confirm that at least one already scanned file is beneath the active directory. Do not open that target or follow an engine-specific pointer file.
- Keep a workflow's normal counts and artifact samples when only its active selection is invalid. Emit a stable issue at the declaration path and leave active path/evidence null.
- Use `CustomWorkflowDeclaration` as the evidence label. Existing workflow declarations that omit the new property remain valid and produce null selection fields.

## Alternatives Considered

- Parse custom engine pointer files: rejected because formats vary and would expand filesystem reads and parsing risk.
- Infer the first or only change directory automatically: rejected because custom workflow directory conventions are not known to BrownPants.
- Reject an entire valid workflow when only its active path is invalid: rejected because selection errors should not erase otherwise useful inventory.
