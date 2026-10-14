# Data Model: MFC Class Inheritance Candidate

The existing `ArchitecturePatternSignal` represents each new result:

| Field | Value |
|---|---|
| `Category` | `MFC class inheritance` |
| `EvidenceKind` | `Candidate` |
| `Language` | `C++` |
| `Path` | Repository-relative source or header path |
| `Line` | First line of the class or struct declaration |
| `Evidence` | Whitespace-normalized declaration text with comments and strings removed |

The signal indicates a lexical match to a recognized MFC base type. It does not represent a parsed or resolved inheritance edge.
