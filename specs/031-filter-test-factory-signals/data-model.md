# Data Model: Architecture Pattern Evidence

This feature changes classification and source evidence for an existing report record; it adds no persisted state or public fields.

## ArchitecturePatternSignal

Defined in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.

| Field | Meaning | Feature behavior |
|---|---|---|
| `Category` | Pattern category, such as `Factory method` | Test-decorated methods do not produce this category based on method name and return type. |
| `EvidenceKind` | Evidence strength label | Remains `Candidate`; syntax is not semantic proof. |
| `Language` | Source language | Remains `C#` or `VB.NET`. |
| `Path` | Repository-relative source path | Unchanged. |
| `Line` | One-based source line | For method findings, points to the declaration/statement, not a preceding attribute or comment. |
| `Evidence` | Compact source excerpt | For method findings, contains the declaration signature. |

The analyzer still uses name and return-type heuristics. Attribute matching is syntactic and can be affected by a user-defined attribute that reuses a recognized framework marker name; reports must retain the existing candidate caveat.
