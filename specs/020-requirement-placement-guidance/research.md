# Research: Requirement Placement Guidance

## Decisions

### Base decisions on explicit attributes instead of keyword guesses

The proposed rule's wording cannot reliably establish its scope or purpose. Require explicit scope (`repository`, `feature`, or `unknown`), durability (`enduring`, `feature`, or `unknown`), and purpose (`principle`, `outcome`, `agent_workflow`, `mixed`, or `unknown`). Repeat these values in the report and explain the resulting recommendation.

### Use transparent rules with a safe clarification path

An enduring repository-wide principle maps to a constitution; a feature-bounded user outcome maps to that feature's spec; operational instructions for agents or contributors map to guidance. Mixed concerns recommend a split. Missing or conflicting values produce clarification questions rather than a confident but unsupported placement.

### Keep recommendations advisory and engine-neutral

The core returns deterministic Markdown or JSON without modifying artifacts. Spec Kit describes the constitution workflow as creating/updating project principles and the specify workflow as creating a feature spec. BrownPants uses those distinctions as adapter examples, while the classifier uses portable destinations. See [Spec Kit constitution command](https://github.com/github/spec-kit/blob/main/templates/commands/constitution.md) and [Spec Kit specify command](https://github.com/github/spec-kit/blob/main/templates/commands/specify.md).

### Preserve human review

This rule-based tool does not semantically interpret prose or decide whether supplied attributes are true. It reports the submitted statement and choices, the rule that matched, and remaining questions. It never writes a constitution or spec automatically.

## Alternatives Rejected

- Guessing from keywords such as “always” or “must”: phrasing does not prove scope or durability.
- Making an LLM mandatory: this weakens determinism and may move the decision criteria without review.
- Automatically editing the selected artifact: a recommendation is not approval to change project governance or requirements.
- Hard-coding the classifier to Spec Kit paths: the underlying distinction applies to any SDD workflow.
