# Contract: BrownPants Bug Evidence Hook

## Registration

- Event: `before_bug_assess`
- Command: `speckit.brownpants.bug-evidence`
- Optional: `true`
- Dependency: Spec Kit's bundled `bug` extension.

## Inputs and output

Use the repository root, pasted bug report text already present in the active conversation, Spec Kit's resolved `BUG_SLUG`/`BUG_DIR`, and an installed BrownPants executable. On success, write Markdown to `<BUG_DIR>/brownpants-evidence.md`. If clues, executable, or safe output location are unavailable, provide an explicit skip status.

## Invariants

- Never fetch URLs, run builds/tests/project code, or evaluate MSBuild.
- Never write outside the active bug directory or silently overwrite the report.
- Never include matched source snippets or claim a match proves cause or test coverage.
- A skipped optional hook does not prevent official assessment.
