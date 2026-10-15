# Research: On-Demand Instruction Sections

**Date**: 2026-10-14

## Findings

- BrownPants already inventories recognized instruction files, section headings, anchors, and source digests in `src/BrownPants.Cli/Analysis/InstructionSourceAnalyzer.cs`.
- The existing response-frame command links to cited source anchors but does not return section text. Its output explicitly says host loading behavior determines whether the linked source is still included.
- VS Code documents agent skills as progressive disclosure: agents discover names and descriptions first, then load skill instructions and referenced resources when needed. It also supports custom agents with model preferences, though host-specific configuration remains outside a portable SDD document.
- OpenAI's skills documentation similarly describes loading a skill's instructions when invoked and following resource references as needed. This supports the host-managed retrieval pattern, not a guarantee that arbitrary instruction files are automatically unloaded.

## Design Implications

1. Add a deterministic retrieval operation over an explicitly chosen recognized source and anchor. The caller, not an opaque heuristic, chooses what to load.
2. Return section content with path, heading, anchor, and line range so an agent can cite the original file and reviewers can check extraction boundaries.
3. Reuse inventory's anchor convention and source size/safety limits; do not allow path traversal, reparse-point escapes, network access, or project evaluation.
4. Keep the capability engine-neutral. An SDD engine or host adapter may invoke it, but BrownPants cannot prevent hosts from preloading the same files or promise token savings.

## Sources

- [VS Code agent skills](https://code.visualstudio.com/docs/agent-customization/agent-skills)
- [OpenAI Skills](https://developers.openai.com/api/docs/guides/tools-skills)
