# Verification: On-Demand Instruction Sections

## Acceptance criteria baseline

- Frozen digest: `9368ca4fbcb0b83160deac1367fc2145e98bd29748769e7a16a8df6932167632`
- The final `acceptance-criteria-hash.ps1` check passed with the same digest.
- No acceptance scenarios or success criteria changed after baseline initialization.

## Test-first evidence

- Before implementation, the focused suite showed 4 expected failures for section retrieval/fence handling and 7 passing existing tests.
- After implementation, focused instruction-analyzer tests passed (14 tests).
- Final `dotnet build BrownPants.sln`: passed, 0 warnings and 0 errors.
- Final `dotnet test BrownPants.sln`: passed, 90 tests, 0 failed, 0 skipped.
- `tests/SpeckitExtensionTests.ps1`: passed, including synchronized guidance and live Spec Kit CLI checks.
- `tests/OpenSpecInstallerTests.ps1`: passed.
- Quickstart JSON invocation on `AGENTS.md#repository-guidelines`: returned schema version 1, source path, heading, anchor, lines 1-30, and only the selected section content.

## Acceptance mapping

The xUnit fixtures exercise nested section boundaries, duplicate anchors, fenced heading examples, metadata/format consistency and determinism, missing anchors, traversal paths, unsupported encoding, oversized sources, and symbolic-link rejection where link creation is supported. Adapter checks verify that all maintained Spec Kit context instructions describe retrieval and the host limitation.

The results verify local retrieval and host guidance. They do not prove that an AI host avoids preloading other instruction files or that provider token usage decreases.
