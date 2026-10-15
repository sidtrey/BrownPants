# Shared Agent Prompt

The exact task prompt used in both conditions is:

> Implement the shared Spec Kit task in `specs/046-brownfield-sdd-comparison/experiment/spec.md`, following its `plan.md` and `tasks.md`. Read the relevant source/tests, make the smallest code change that satisfies the frozen acceptance criteria, and report the changed paths, concise diff, remaining concerns, and criterion status. Do not edit the specification, plan, tasks, or acceptance hash. The experiment driver has already recorded the same focused failing regression in `TestResults/seed-failure.md` in both worktrees. Do not run builds, tests, package restore, dependency updates, or network commands; the experiment driver runs identical focused and full Release tests after both agents exit. Leave the validation task incomplete and do not claim a test passed.

If `TestResults/brownpants-context.md`, `TestResults/brownpants-investigate.md`, or `TestResults/brownpants-impact.md` exist, read them as supplementary repository evidence and verify relevant leads against source. Their absence means no BrownPants reports were supplied; continue with normal source inspection.

The baseline receives this prompt and the shared Spec Kit feature artifacts. The assisted condition receives the same prompt and artifacts, plus the BrownPants context, investigation, and impact reports captured in `TestResults/`. BrownPants acceptance hash/evidence checks are also run by the experiment driver for the assisted condition. These additions are the declared treatment difference.
