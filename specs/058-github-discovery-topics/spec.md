# Feature Specification: GitHub Discovery Topics

**Feature Branch**: `058-github-discovery-topics`
**Created**: 2026-10-15
**Status**: Draft
**Input**: Queue item 19: improve GitHub repository visibility with accurate topics, especially brownfield analysis and supported .NET/MFC scenarios.

## User Scenarios & Testing

### User Story 1 - Find BrownPants by its real strengths (Priority: P1)

A developer evaluating tools for an existing or legacy codebase should be able to discover BrownPants from the repository's public topics. The topics should emphasize brownfield understanding and legacy modernization while also identifying supported development workflows and language ecosystems.

**Why this priority**: Brownfield analysis is the product's primary purpose, and repository topics are currently absent.

**Independent Test**: Compare the public About topics with the committed README and implemented analysis capabilities; confirm every topic describes the product accurately and the list includes its primary use case.

**Acceptance Scenarios**:

1. **AC-001** **Given** the public repository has no topics, **When** its metadata is updated, **Then** its About section exposes a concise topic set that clearly identifies BrownPants with brownfield analysis and legacy modernization.
2. **AC-002** **Given** BrownPants supports C#, Visual Basic, C/C++, MFC candidate analysis, and spec-driven workflows, **When** users inspect its topics, **Then** those relevant technologies and workflows are discoverable without implying unsupported runtime or compiler coverage.
3. **AC-003** **Given** GitHub topic constraints, **When** topics are saved, **Then** all names are lowercase, use only letters, numbers, or hyphens, are at most 50 characters, and total no more than 20.

## Edge Cases

- The repository already has topics when implementation begins; retain only ones still accurate and preserve unrelated valid topics.
- GitHub metadata permissions are insufficient; do not claim the update succeeded without reading the saved public topic list.
- A topic suggests stronger analysis than BrownPants actually provides; omit it.

## Requirements

### Functional Requirements

- **FR-001**: The reviewed topic set MUST include the core terms `brownfield`, `legacy-modernization`, `code-analysis`, and `repository-analysis`.
- **FR-002**: The reviewed topic set MUST also include `spec-driven-development`, `spec-kit`, `dotnet`, `csharp`, `visual-basic`, `cpp`, and `mfc` to reflect the supported workflows and brownfield ecosystems.
- **FR-003**: Every topic MUST be relevant, accurate, and compliant with GitHub's published naming and count constraints.
- **FR-004**: Topics MUST NOT imply that lexical MFC/C++ findings are compiler-resolved or that BrownPants requires an LLM.
- **FR-005**: The final public topic list MUST be read back and compared with the reviewed intended list before the task is recorded as complete.
- **FR-006**: Verification records MUST NOT contain authentication tokens or other credentials.

## Key Entities

- **Repository topic**: A public label that classifies BrownPants by purpose, supported ecosystem, or workflow.

## Success Criteria

- **SC-001**: The public repository has at least one topic for brownfield analysis, one for legacy modernization, and one for spec-driven development.
- **SC-002**: Every published topic is justified by committed documentation or implemented capability, and the list complies with GitHub's current constraints.
- **SC-003**: A read-back of GitHub metadata exactly matches the reviewed topic list; no update is reported as complete without this evidence.
- **SC-004**: No credentials are written to repository files, command output, or committed verification evidence.

## Assumptions

- The repository is public and has no current About topics; both were verified before planning.
- Topics are metadata-only; source behavior, release version, and repository visibility are unchanged.
- Use the smallest set that covers core purpose, supported ecosystems, and SDD discovery; avoid speculative or generic AI labels.
