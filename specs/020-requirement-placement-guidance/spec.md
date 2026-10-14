# Feature Specification: Requirement Placement Guidance

**Feature Branch**: `020-requirement-placement-guidance`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Help contributors decide whether a proposed software rule belongs in a project constitution, a feature specification, or agent/contributor guidance.

## User Scenarios & Testing

### User Story 1 - Recommend where a proposed rule belongs (Priority: P1)

A contributor has a rule they want to add and is unsure whether it is a durable project principle, a feature-specific outcome, or an instruction for agents and contributors. They want an explained recommendation instead of having to learn SDD terminology first.

**Why this priority**: Misplacing a rule can make it hard to find, inconsistently applied, or incorrectly treated as a requirement for every feature.

**Independent Test**: Submit examples for a durable cross-feature principle, a feature-specific user outcome, and an agent workflow instruction; verify the recommended destination and rationale match the stated scope and intent.

**Acceptance Scenarios**:

1. **Given** a durable principle intended to govern multiple features, **When** its purpose, scope, and lifetime are provided, **Then** the tool recommends the constitution and explains which inputs support the recommendation.
2. **Given** a concrete user-visible outcome limited to one proposed feature, **When** its purpose, scope, and lifetime are provided, **Then** the tool recommends that feature's specification and explains why it is not a project-wide principle.
3. **Given** an instruction about how an agent or contributor should perform work, **When** it is classified, **Then** the tool recommends agent/contributor guidance and distinguishes it from product behavior.

### User Story 2 - Handle mixed or incomplete rules without false certainty (Priority: P1)

A contributor enters a statement that combines a general principle with a one-feature change, or lacks enough information to classify. They need the tool to expose the ambiguity and show what to clarify or split.

**Why this priority**: A confident but unjustified placement would reinforce the confusion the feature is intended to resolve.

**Independent Test**: Submit incomplete and mixed-purpose statements; verify the tool asks focused questions or recommends splitting the rule rather than silently choosing one destination.

**Acceptance Scenarios**:

1. **Given** required classification details are missing, **When** guidance is requested, **Then** the tool returns the missing questions and no unsupported final recommendation.
2. **Given** one statement contains both a lasting cross-feature rule and a feature-specific outcome, **When** it is classified, **Then** the tool recommends splitting the content and identifies what belongs in each artifact.
3. **Given** the statement and supplied details conflict, **When** guidance is requested, **Then** the conflict and the evidence behind each plausible destination are reported.

### User Story 3 - Use the guidance in any SDD workflow (Priority: P2)

A contributor or SDD engine wants to use the same placement guidance without depending on Spec Kit or a particular programming language.

**Why this priority**: The decision is about requirement scope and durability, which apply across repositories and SDD workflows.

**Independent Test**: Request Markdown and JSON guidance for the same inputs; verify equivalent recommendations, stable evidence, and no repository modifications.

**Acceptance Scenarios**:

1. **Given** identical candidate text and classification inputs, **When** guidance is requested repeatedly, **Then** the result is deterministic and available in human-readable and machine-readable form.
2. **Given** any SDD engine or source language, **When** a candidate rule is assessed, **Then** the classification uses the same engine-neutral rubric and does not assume a language or build system.

## Edge Cases

- A broad goal may need a constitution principle plus a feature spec containing a concrete, testable outcome.
- A coding convention may be long-lived but operational; long lifetime alone does not make it a constitution principle.
- A feature rule may later prove useful across the project; the initial recommendation should not silently promote it.
- The statement may be too vague to determine whether it governs product behavior or agent behavior.
- Recommendation inputs may be incomplete, contradictory, or outside the supported categories.

## Requirements

### Functional Requirements

- **FR-001**: Contributors MUST be able to submit a proposed rule and identify its intended scope, durability, and purpose.
- **FR-002**: The tool MUST recommend constitution, feature specification, agent/contributor guidance, split across artifacts, or more information based on an explicit decision rubric.
- **FR-003**: Every recommendation MUST state which supplied facts support it and explain the distinction between durable cross-feature principles, feature-specific outcomes, and operational instructions.
- **FR-004**: Missing or contradictory classification inputs MUST produce focused clarification prompts or a split recommendation rather than an unsupported single destination.
- **FR-005**: The tool MUST NOT modify the constitution, feature specs, or instruction files as a side effect of a recommendation.
- **FR-006**: The same inputs MUST produce deterministic recommendations and rationale in Markdown and JSON.
- **FR-007**: The core rubric MUST be independent of programming language, SDD engine, and LLM availability.
- **FR-008**: Reports MUST explain that placement guidance is decision support; the contributor remains responsible for reviewing and editing project artifacts.
- **FR-009**: The Spec Kit context adapter MUST direct contributors to the placement guidance when they ask where a proposed rule belongs, and MUST require the supplied attributes rather than silently guessing them.

### Key Entities

- **Candidate rule**: Proposed text with intended scope, expected lifetime, and primary purpose.
- **Placement recommendation**: Destination or split recommendation, supporting evidence, caveats, and any clarification questions.
- **Placement rubric**: Explicit rules that map candidate attributes to recommendations.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All documented canonical examples for constitution, feature spec, and agent guidance receive the expected recommendation and rationale.
- **SC-002**: Incomplete, conflicting, and mixed-purpose examples are surfaced without presenting an unsupported single destination as certain.
- **SC-003**: Markdown and JSON results for identical input express the same destination, rationale, and clarification needs.
- **SC-004**: Repeated runs with identical inputs produce byte-identical output, and the decision process performs no repository writes or external calls.
- **SC-005**: A contributor can distinguish where a durable principle, its feature-specific implementation outcome, and its operational agent instructions belong from one report.

## Assumptions

- Contributors can characterize a proposed rule's scope, durability, and purpose through a small set of explicit choices; prose alone is not reliable evidence for a definitive placement.
- Recommendations should favor clarification when the structured inputs conflict, even if the candidate wording contains suggestive keywords.
- The tool explains trade-offs and may recommend splitting a statement across multiple artifacts.
- SDD-specific integration can be added through adapters; the core result remains portable.
