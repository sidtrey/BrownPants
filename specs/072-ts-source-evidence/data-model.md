# Data Model: JavaScript and TypeScript Source Evidence

All paths are repository-relative with `/` separators. Lines and columns are one-based. Records are immutable report evidence and are sorted deterministically.

## Declaration

`Language`, `Path`, `Line`, `Column`, `Kind`, `Name`, optional `Container`, optional `ExportKind`, and `EvidenceKind`. `ExportKind` is `named` only when an explicit `export` modifier is recognized. `EvidenceKind` is `syntax-candidate`; declarations do not claim type correctness.

## Module relationship

`SourcePath`, `SourceLine`, `Specifier`, `Kind` (`import`, `re-export`, `dynamic-import`, `require`), `Status`, optional `TargetPath`, `CandidateTargetPaths`, and `Reason`. Status is `syntax-resolved`, `ambiguous`, or `unresolved`.

## Call relationship

`SourcePath`, `SourceLine`, `Caller`, `Expression`, `Status`, optional `TargetPath`/`TargetLine`/`TargetName`, `CandidateTargets`, and `Reason`. A status of `syntax-resolved` means one supported in-repository declaration matches the static syntax; it is never Roslyn/compiler binding or runtime dispatch.

## Analysis summary

`EvidenceLevel`, source-file count, exact record totals by family, sampled records, omitted counts, bounded `Issues`, and explicit `EvidenceLimits`. Each family uses the CLI sample limit; counts refer to observed records before sampling. Malformed or escaping config inputs are represented as issues without external reads.
