# Data Model: Native Flow Candidate

## NativeFlowRelation

| Field | Type | Meaning |
|---|---|---|
| `Id` | string | Stable identifier derived from relation kind and source/target identity |
| `Kind` | string | `mfc-dispatch` or `qualified-native-call` |
| `Status` | string | `candidate`, `unresolved`, or `ambiguous` |
| `SourcePath` | string | Repository-relative source file containing the map/call site |
| `SourceLine` | integer | One-based source location of the map entry or call |
| `SourceSymbol` | string | Handler or containing class/method when available |
| `TargetPath` | nullable string | Unique matching definition path, otherwise null |
| `TargetLine` | nullable integer | One-based unique target-definition line, otherwise null |
| `TargetSymbol` | string | Requested handler/callee identity |
| `CommandId` | nullable string | MFC message/command identifier for dispatch relations |
| `CandidateTargetCount` | integer | Number of matching definitions considered |
| `Evidence` | string | Sanitized, bounded source evidence; no source excerpts beyond the matched line |

### Validation and Identity

- A `candidate` relation has exactly one matching definition and a non-null target path and line.
- `unresolved` has no matching definition; `ambiguous` has more than one. Both retain source identity and candidate count.
- Native-flow candidate edges never use managed semantic status `resolved`.
- Identity and serialization are deterministic for equal repository content and options. Samples are capped by the existing report limit, with total and omitted counts retained.

## NativeFlowAnalysis

Contains total relation count, emitted relation samples, unresolved count, ambiguous count, and evidence limits. It is nested additively in the existing repository architecture analysis and consumed by Markdown and structured evidence output.
It also reports the number of native source files scanned; Markdown omits the native-flow section when this count is zero.
