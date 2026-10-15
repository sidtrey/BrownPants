# Acceptance Criteria Review: Local Delegate Impact

**Status**: Approved by maintainer

**Reason**: The release MVP is explicitly gated on proving a measurable advantage over vanilla SDD. The existing controlled comparison showed no finding-quality improvement. This proposal adds a frozen, replicated comparison criterion so the delegate-impact feature cannot be presented as superior without measurable evidence.

- **Previous digest**: `7a0af3da3bf45d40742d2dfc0b1b4014f4372a2a0d97a5eec2e72bc8a37b38c3`
- **Proposed digest**: `fac2f20e2e615324c06ce01b3085444948d17ad4f58c05b8cce016ca9a2150bc`
- **Changed IDs**: `AC-006`, `SC-006`

**Proposed gate**: Three fresh matched baseline/assisted pairs, identical frozen source/task/model/CLI/sandbox/schema, counterbalanced execution order, source-derived scoring, pooled actionable managed-impact recall gain of at least 20 percentage points with no precision loss, at least two pairs favoring BrownPants, and all six independent post-fix checks passing. Preserve the raw runs, usage, scores, hashes, and limitations. If the gate is missed, describe the result as not demonstrating superiority.

**Review outcome**: Approved as written by the repository maintainer in chat on 2026-10-15. Apply the `acceptance-criteria-reviewed` pull request label after this change is opened for review.

