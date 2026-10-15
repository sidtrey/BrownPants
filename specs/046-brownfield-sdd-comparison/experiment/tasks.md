# Tasks: Restore Test-Directory Path Classification

## Implementation

- [X] T001 [US1] Confirm the seeded regression fails for a fixture under an exact test directory; keep assertions that an actual BrownPants source path is not a test and a conventional test filename is.
- [X] T002 [US1] Restore exact test-directory component recognition in `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` without changing filename rules.
- [X] T003 [US1] Experiment driver runs the focused RepositoryScanner tests and full Release solution suite after both agent sessions; report exact results without editing task acceptance criteria.

## Dependencies

- T001 and T002 implement the shared acceptance scenarios; T003 validates both.
