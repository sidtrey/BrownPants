# Data Model: Workspace Diagnostic Message

A workspace diagnostic is existing bounded text captured while loading semantic projects. Its public representation stays a string.

| Portion | Rule |
| --- | --- |
| Repository-local absolute path | Replace with normalized repository-relative path using `/` separators |
| External absolute path | Replace with `[external-path]` |
| Non-path diagnostic text | Preserve verbatim |
| Issue count and sample | Preserve existing total and caller-selected cap |

Containment is determined from canonical paths and path-segment boundaries. A sibling such as `repo-copy` is external to `repo` even though its string begins with the same characters.
