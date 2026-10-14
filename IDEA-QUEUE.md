# BrownPants Idea Queue

Ideas are kept in the order received. The current active implementation remains in its Spec Kit feature artifacts; adding an idea here does not interrupt it.

## Queue

1. **Instruction-context framing — implemented in feature 019** — Map recognized and custom instruction files, create cited response frames, detect changed sources, and explain that host configuration determines context savings.
2. **Constitution vs. spec guidance — next** — Help a contributor decide whether a proposed rule belongs in the project constitution, a feature spec, or agent guidance. Base the recommendation on scope, durability, and whether it describes a cross-feature principle or a feature-specific outcome; explain the reasoning and suggest where to put it.
3. **Acceptance-criteria integrity — initial gate added** — Keep acceptance criteria fixed and verifiable throughout a task so an LLM cannot quietly move the goalposts. Feature 019 now has a SHA-256 baseline, verifier, CI check, and Spec Kit before/after guidance. Explore stronger task-level enforcement and reviewed baseline updates.
4. **Lazy-load relevant context** — Explore how agents can avoid repeatedly loading every instruction file when only a few details matter. Map host capabilities, retrieval behavior, and whether BrownPants can help select and load only relevant source sections while preserving access to the complete canonical guidance.
