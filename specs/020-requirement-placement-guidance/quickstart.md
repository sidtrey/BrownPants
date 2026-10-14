# Quickstart: Requirement Placement Guidance

Submit the proposed rule and select its scope, durability, and purpose. Use `unknown` when you cannot determine an attribute; the tool will return clarification questions.

```powershell
brownpants placement --rule "All features must preserve user data on failure" --scope repository --durability enduring --purpose principle
brownpants placement --rule "A user can export this report as JSON" --scope feature --durability feature --purpose outcome --format json
brownpants placement --rule "Before planning, inspect the active repository" --scope repository --durability enduring --purpose agent_workflow
```

The first example recommends the constitution, the second a feature specification, and the third agent/contributor guidance. Use `purpose mixed` when a statement combines a general principle and a specific implementation outcome; BrownPants will recommend splitting them. Identical inputs produce identical results.

Review the rationale and submitted attributes before editing any artifact. BrownPants does not infer intent from keywords or modify files. The complete JSON response contract is in `contracts/placement-guidance.schema.json`.
