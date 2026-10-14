# Feature Specification: Instruction Context Frames

**Feature Branch**: `019-instruction-context-framing`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Create a compact core instruction that frames agent responses around important context points while preserving access to full instruction details, reducing repeated prompt load where the agent host allows on-demand retrieval.

## User Scenarios & Testing

### User Story 1 - Understand the repository's instruction set (Priority: P1)

A contributor inherits a repository with many instructions spread across agent, SDD, and project files. They need a reliable map of which files and sections apply, including the approximate context cost, before consolidating anything.

**Why this priority**: A frame built from incomplete discovery can silently omit an active instruction or misstate which rule applies.

**Independent Test**: Scan a temporary mixed-language repository with global and path-scoped instruction files plus custom files; verify stable paths, scopes, headings, sizes, and explicit unsupported-file limits.

**Acceptance Scenarios**:

1. **Given** recognized instruction files and explicitly selected custom files, **When** a contributor requests an instruction map, **Then** each source is listed with repository-relative path, applicable scope, section anchors, and size estimates.
2. **Given** nested or path-scoped rules, **When** the map is produced, **Then** their narrower applicability is distinguished from repository-wide guidance.
3. **Given** an unreadable, malformed, or unsupported source, **When** the map is produced, **Then** the limitation is reported without inventing its contents or losing other sources.

### User Story 2 - Frame responses without discarding detail (Priority: P1)

A contributor wants the assistant to keep a short set of priorities in view while retaining the full instructions for precise questions and edge cases.

**Why this priority**: The frame should reduce repeated context while preserving the original guidance as the authority.

**Independent Test**: Generate a frame from fixture instructions; verify every frame point links to source sections, mandatory constraints and conflicts remain visible, and original files are byte-identical.

**Acceptance Scenarios**:

1. **Given** a mapped instruction set, **When** a contributor creates a response frame, **Then** it contains concise response priorities and exact source references for the full details behind each point.
2. **Given** a task that needs a specific instruction, **When** the assistant uses the frame, **Then** it is directed to open the referenced source section instead of guessing omitted detail.
3. **Given** contradictory or unresolvable instructions, **When** the frame is generated, **Then** the conflict and source references are surfaced for review rather than silently reconciled.
4. **Given** an existing frame or changed source instructions, **When** a contributor regenerates the frame, **Then** the tool identifies the existing output and stale sources and does not silently overwrite the frame or source files.

### User Story 3 - Use a frame in an SDD workflow (Priority: P2)

A contributor wants an SDD agent to use the frame as its compact starting context and retrieve authoritative details only when they are relevant to the current phase or task.

**Why this priority**: A reusable frame is useful only if agent instructions explain how to consult it and when to retrieve its referenced detail.

**Independent Test**: Install the Spec Kit adapter in a disposable project, generate a frame, and verify lifecycle guidance prioritizes the frame while directing the agent to read cited source sections when needed.

**Acceptance Scenarios**:

1. **Given** a frame is available, **When** a Spec Kit phase starts, **Then** the agent uses its response priorities and checks linked details relevant to that phase.
2. **Given** the host automatically injects all source files regardless of the frame, **When** the frame is used, **Then** the report explains that the host may not realize token savings until its instruction loading is configured to use the frame.

## Edge Cases

- Agent instruction conventions change or a repository uses custom filenames; contributors can provide explicit source paths and see which files were selected.
- A directory contains many path-specific rules; inventory remains bounded and preserves scope rather than flattening all rules into global instructions.
- An instruction file contains secrets, personal data, or executable-looking text; the frame must not copy sensitive literals or execute content.
- The requested frame budget is too small to represent all critical constraints; report that the budget cannot be met without omission and preserve the sources.
- A source changes after frame creation; staleness is visible by a source digest or equivalent reviewable marker.
- A generated summary is ambiguous; exact source section references remain authoritative and the ambiguity is not resolved by guesswork.

## Requirements

### Functional Requirements

- **FR-001**: Contributors MUST be able to inventory recognized repository instruction files and explicitly include custom source paths.
- **FR-002**: The map MUST identify each source's repository-relative path, applicable scope, section headings or equivalent anchors, and source size; estimates MUST be labeled approximate when they do not use the active model tokenizer.
- **FR-003**: Contributors MUST be able to create a compact, portable Markdown response frame with a configurable word or token budget.
- **FR-004**: Every frame point MUST link to one or more exact source files and sections; the frame MUST direct agents to retrieve the full source section when a task needs details.
- **FR-005**: The frame MUST preserve priority distinctions and surface contradictory, unresolved, or unclassified source instructions for contributor review.
- **FR-006**: Original instructions MUST remain unchanged; the tool MUST NOT silently overwrite an existing frame or source file.
- **FR-007**: The frame MUST include enough source identity information to determine when a referenced source has changed since generation.
- **FR-008**: Reports MUST state that the frame is a navigation and response-framing aid, not a guarantee of model memory, instruction compliance, or token savings across host environments.
- **FR-009**: The core inventory and frame format MUST work across languages and SDD engines; agent-specific instructions MUST remain in adapters.
- **FR-010**: Malformed or unsupported instruction files MUST be reported as limitations while valid sources remain available.

### Key Entities

- **Instruction source**: A recognized or explicitly selected repository file containing agent, project, or workflow guidance, with path, scope, section anchors, and content identity.
- **Response frame**: A concise prioritized instruction layer with links to authoritative detailed source sections and retrieval guidance.
- **Frame point**: One concise response priority or contextual reminder linked to one or more instruction sources.
- **Frame source map**: The relationship between each frame point and the source sections that support it.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In a fixture containing supported global, scoped, and custom instruction files, every expected source and heading is listed with stable repository-relative references.
- **SC-002**: A generated frame stays within the requested budget when the constraints can be represented safely; otherwise, it reports the shortfall rather than silently dropping critical rules.
- **SC-003**: Every frame point has a verifiable source reference, and all unresolved conflicts are listed for review.
- **SC-004**: Frame generation leaves every input instruction file byte-identical and never overwrites an existing frame without explicit approval.
- **SC-005**: A source edit after generation is detected as stale before the frame is presented as current.
- **SC-006**: A contributor can identify whether the frame can reduce prompt loading in the selected host from the report's explicit host caveat.

## Assumptions

- The compact frame is an additional, reviewable artifact; original instructions remain canonical and available for on-demand retrieval.
- A model may help draft semantic summaries, but BrownPants must keep source mapping, stale detection, and safety checks explicit and reviewable.
- Repository instruction discovery is language-agnostic and combines known common conventions with explicit user-selected paths.
- Host-specific changes that stop automatic injection of original files are opt-in and documented; BrownPants does not rewrite agent configuration or source instructions automatically.
