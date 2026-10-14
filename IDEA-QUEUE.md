# BrownPants Idea Queue

Ideas are kept in the order received. The current active implementation remains in its Spec Kit feature artifacts; adding an idea here does not interrupt it.

## Queue

1. **Instruction-context framing — implemented in feature 019** — Map recognized and custom instruction files, create cited response frames, detect changed sources, and explain that host configuration determines context savings.
2. **Constitution vs. spec guidance — implemented in feature 020** — Recommend constitution, feature spec, or agent guidance from explicit scope, durability, and purpose inputs; flag conflicts and suggest splitting mixed rules. The `placement` CLI keeps this rubric deterministic and engine-neutral.
3. **Acceptance-criteria integrity — next; initial gate added** — Keep acceptance criteria fixed and verifiable throughout a task so an LLM cannot quietly move the goalposts. Feature 019 now has a SHA-256 baseline, verifier, CI check, and Spec Kit before/after guidance. Explore stronger task-level enforcement and reviewed baseline updates.
4. **Lazy-load relevant context** — Explore how agents can avoid repeatedly loading every instruction file when only a few details matter. Map host capabilities, retrieval behavior, and whether BrownPants can help select and load only relevant source sections while preserving access to the complete canonical guidance.
5. **Why can’t LLMs lazy-load context?** — Explore why prompt payload size affects cost when models often use only a small portion of the supplied context. Could systems load relevant instructions or source sections on demand, while still allowing the model to find what it needs? Consider current model/API limits and whether BrownPants could help.
