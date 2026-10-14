# Data Model: Requirement Placement Guidance

## Candidate Rule

- `Rule`: non-empty proposed requirement text, preserved in output.
- `Scope`: `repository`, `feature`, or `unknown`.
- `Durability`: `enduring`, `feature`, or `unknown`.
- `Purpose`: `principle`, `outcome`, `agent_workflow`, `mixed`, or `unknown`.

## Placement Report

- `SchemaVersion`: integer version of the JSON contract.
- `Recommendation`: `constitution`, `feature_spec`, `agent_guidance`, `split`, or `clarify`.
- `Destinations`: ordered artifact destinations recommended by the matched rule.
- `Rationale`: ordered explanations that name the submitted attributes and matched rule.
- `Questions`: clarification prompts when unknown or conflicting information prevents a single recommendation.
- `EvidenceLimits`: report that attributes were supplied by the requester and were not inferred from prose.

## Decision Rules

| Inputs | Recommendation | Rationale |
|---|---|---|
| principle + repository + enduring | constitution | Durable cross-feature constraint |
| principle + feature + feature-bounded | feature spec | Limited to a particular feature; generalize later only if intended |
| outcome + feature | feature spec | Testable behavior/result for one feature |
| outcome + repository + enduring | split | State the invariant in the constitution and concrete outcomes in feature specs |
| principle or outcome + repository + feature-bounded | clarify | Scope and lifetime disagree; ask whether the rule is temporary or truly repository-wide |
| principle + feature + enduring | feature spec | The rule is feature-scoped even if it remains in force for that feature |
| outcome + repository + feature-bounded | clarify | Ask which feature owns the temporary outcome |
| agent workflow | agent guidance | Operational directions belong with contributor/agent instructions |
| mixed | split | Separate distinct principle, outcome, or procedure statements |
| unknown or incompatible dimensions | clarify | Ask about missing or conflicting scope, durability, or purpose |

The analyzer follows this table in order: unknown values first, then `mixed`, then explicitly conflicting dimension pairs, then the purpose-specific rules. No text keyword overrides structured inputs. An enduring feature-scoped principle remains in that feature's spec; durability alone does not promote it to the constitution.
