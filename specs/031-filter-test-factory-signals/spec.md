# Feature Specification: Filter Test Methods from Factory Signals

**Feature Branch**: `031-filter-test-factory-signals`

**Created**: 2026-10-15

**Status**: Draft

**Input**: Self-inspection found xUnit test methods reported as factory candidates, with `[Fact]` shown as the evidence instead of the method declaration.

## User Scenarios & Testing

### User Story 1 - Trust .NET factory-method findings (Priority: P1)

A developer reviewing a BrownPants report wants factory-method candidates to point to production code that may construct objects, not test methods whose names happen to start with `Create`, `Build`, or `Resolve`. The report should identify the method declaration so the developer can verify the lead quickly.

**Why this priority**: False positives waste review time and can make users distrust BrownPants' architecture findings.

**Independent Test**: Scan temporary C# and Visual Basic projects with test methods marked by common .NET test attributes and with attributed production factory methods. Verify the tests are excluded and production factories retain accurate evidence.

**Acceptance Scenarios**:

1. **AC-001** — Given a C# or Visual Basic method marked with a recognized xUnit, NUnit, or MSTest test attribute and named with a factory prefix, when BrownPants reports architecture patterns, then it does not report that method as a factory-method candidate.
2. **AC-002** — Given a production factory method preceded by attributes or documentation, when BrownPants reports it, then the finding's line points to the method declaration and its evidence identifies the signature rather than the preceding attribute or comment.
3. **AC-003** — Given existing void or string helper methods and object-returning production factory methods, when BrownPants scans C# and Visual Basic code, then the existing exclusions and object-factory findings remain unchanged.

## Edge Cases

- Test attributes may be fully qualified, have an `Attribute` suffix, or contain constructor arguments.
- Multiple attributes may appear above a method declaration.
- The same method may also contain a separately recognized DI or interop signal; only its factory-method classification is affected.
- C# and Visual Basic use different keyword casing and syntax for attributes and method declarations.

## Requirements

### Functional Requirements

- **FR-001**: BrownPants MUST recognize standard xUnit (`Fact`, `Theory`), NUnit (`Test`, `TestCase`, `TestCaseSource`), and MSTest (`TestMethod`, `DataTestMethod`) method attributes in C# and Visual Basic, including qualified and `Attribute`-suffixed forms.
- **FR-002**: A method carrying one of those test attributes MUST NOT be reported as a factory-method candidate based only on its method name and return type.
- **FR-003**: Factory-method findings MUST identify the method declaration line and show signature evidence, even when attributes or comments precede the method.
- **FR-004**: BrownPants MUST preserve existing behavior for non-test string/void helpers and object-returning factory methods, and must continue to label syntax-derived findings as candidates.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Fixture scans return zero factory-method findings for all covered xUnit, NUnit, and MSTest test methods in C# and Visual Basic.
- **SC-002**: Attributed production factories in both languages remain source-located candidates whose evidence contains the declared method signature and points to its declaration line.
- **SC-003**: BrownPants self-inspection no longer reports the submission test methods `CreateDraftAsync_SendsOneDraftCreateRequestWithExplicitWorkItems` or `CreateDraftAsync_RejectsEditedPreviewWithoutNetworkRequest` as factory methods.

## Assumptions

- Test method attributes are reliable enough to suppress name-only factory candidates, while other signals from the same method remain available.
- This change is limited to .NET architecture-pattern analysis; language-specific test conventions in other ecosystems are outside this feature.
