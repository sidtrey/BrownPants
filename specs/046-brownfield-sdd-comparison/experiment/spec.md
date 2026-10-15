# Feature Specification: Restore Test-Directory Path Classification

**Feature Branch**: `046-brownfield-sdd-comparison/experiment`
**Status**: Frozen benchmark input
**Purpose**: Shared implementation task given to both comparison conditions.

## User Scenarios & Testing

### User Story 1 - Keep tests located by their directory (Priority: P1)

A developer scans a repository containing test fixtures whose names do not end in `Test` or `Tests`. BrownPants should still recognize files under an exact `test` or `tests` directory as test-path candidates, while keeping ordinary names containing those letters from being misclassified.

**Independent Test**: Add and run a focused regression test for directory-based recognition, substring false positives, and conventional test-file naming through the existing repository scanner test suite.

**Acceptance Scenarios**:

1. **AC-001** Given `tests/fixtures/invoice.json`, when `RepositoryScanner.IsTestPath` classifies the path, then it returns true because `tests` is a complete path component.
2. **AC-002** Given `src/BrownPants.Cli/Analysis/RepositoryScanner.cs`, when the path is classified, then it returns false because no path component or filename marks it as a test.
3. **AC-003** Given `src/InvoiceTests.cs`, when the path is classified, then it returns true under the existing filename convention.

## Requirements

- **FR-001**: Preserve exact case-insensitive `test` and `tests` directory-component recognition.
- **FR-002**: Do not classify ordinary paths solely because a component contains `test` as a substring.
- **FR-003**: Preserve existing test filename classification rules.
- **FR-004**: Add regression coverage in `tests/BrownPants.Tests/RepositoryScannerTests.cs` and use the existing Release test suite.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The focused regression passes for all three scenarios after the fix.
- **SC-002**: The complete `dotnet test BrownPants.sln -c Release --no-restore` command passes in each condition.
- **SC-003**: The final diff changes only the scanner and relevant tests for this issue, unless the run explicitly explains a justified additional change.

## Assumptions

- The shared seed patch removes only the directory-component branch from the real classifier and adds the same failing regression test to each worktree. Regression paths use an actual BrownPants source path rather than a fabricated controller file.
- The task does not require project builds, MSBuild evaluation, or network access during inspection.
