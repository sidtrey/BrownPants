# Acceptance Criteria Review

## Comparison feature

- Previous SHA-256: `b149fde3254dcebeae52af28a791984fe0e91ee7d24ea34b24a152169038d814`
- Proposed SHA-256: `7c3da79a20458f759f68b18b5ef87eb6077b3323d2de1fdc15a0a877b497c407`
- Reason: remove an irrelevant environment-specific reporting requirement from AC-006. The requested report should omit private host details and raw session logs without discussing unrelated local restrictions.
- Changed criterion: AC-006.

## Shared bug-fix task

- Previous SHA-256: `e1e232fd68ed5fd83a6f4d837cbeb09e8ac0461a246b8dcd31e4403b30674c0`
- Proposed SHA-256: `ee166e7921fa8163d5744acdea4d0e0f2a556f680c96e93edb4b3e05831e61cb`
- Reason: replace a fabricated `ContestController.cs` path with the real BrownPants source path `src/BrownPants.Cli/Analysis/RepositoryScanner.cs`, so the comparison no longer depends on a made-up code file.
- Changed criterion: AC-002.

These refinements were made before either comparison run. The maintainer requested a real source-level bug and the criteria now name the real path used by the experiment. No implementation result was available when the hashes were refreshed.
