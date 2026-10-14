# Data Model: Spec Kit Bug Assessment Evidence

## Bug Report Clue

- Distinctive literal text from pasted report content already available in the conversation.
- Up to 20 clues, each within the CLI's 1000-character limit.
- Exclude secrets, personal data, and generic terms; never fetch URLs.
- Preserve the clue with each matching location through the existing investigator.

## Bug Evidence Report

- **Path**: `.specify/bugs/<slug>/brownpants-evidence.md`
- **Content**: Existing Markdown investigation output with clues, repository-relative paths, lines, match kinds, test-candidate labels, limits, and diagnostics.
- **Source text**: Matched source line contents are omitted.
- **Meaning**: Navigation leads only; not proof of behavior, cause, or regression coverage.

## Bug Directory

- Owned and created by Spec Kit at `.specify/bugs/<slug>/` before the pre-hook runs.
- BrownPants writes only its evidence file there and does not silently replace an existing report.
