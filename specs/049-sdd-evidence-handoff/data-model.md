# Data Model: SDD Evidence Handoff

- **EvidenceReport**: schema version, repository root, compact repository context, finding list, limitations, and completeness counts.
- **EvidenceFinding**: stable ID, category, status, signal, repository-relative path, optional line, optional caller/target signatures, and evidence text.
- **Category**: `managed-call`, `architecture-candidate`, `framework-candidate`, or `test-identification`.
- **Status**: `resolved` only for Roslyn-resolved managed calls; `candidate` for architecture/framework/test-path observations.
- **Completeness**: total and emitted counts by category, truncation indicator, and semantic project coverage when requested.

Finding identity uses category, path, line, signal, and (for managed calls) caller and target signatures. Identical findings are emitted once; overloads and distinct source locations remain separate.
