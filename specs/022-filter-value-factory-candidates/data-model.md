# Data Model: Filter Value-Returning Factory Candidates

No report fields or persisted data change.

The existing architecture signal remains:

- `Category`: `Factory method`
- `Language`: `C#` or `VB.NET`
- `Path` and `Line`: source location
- `EvidenceKind`: `Candidate`
- `Evidence`: method declaration excerpt

The analyzer's inclusion decision uses the source-declared return type. String-returning methods with a recognized factory prefix are omitted; object-returning methods continue to produce the existing signal.