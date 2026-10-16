# Verification: JavaScript and TypeScript Source Evidence

## Implementation checks

- `dotnet build BrownPants.sln -c Release --no-restore` passed with zero warnings and errors.
- `dotnet test BrownPants.sln -c Release --no-build --no-restore` passed all 379 tests; `scripts/test.ps1 -NoBuild` also passed and cleaned its managed run directory.
- Cyclic barrel export coverage verifies two resolvable module edges, a deterministic report, and an unresolved imported call with no fabricated target.
- Acceptance hash verified: `1fb91210eebfc19a53e4883965a3585abffb4a4023c15339b04bd454d98cab52`.
- Spec Kit extension, OpenSpec installer, Copilot prompt, Cursor command, Codex usage parser, agent skill, acceptance integrity, acceptance hash, and test artifact cleanup scripts passed.
- `dotnet restore BrownPants.sln --locked-mode` passed. `dotnet list BrownPants.sln package --vulnerable --include-transitive` reported no vulnerable packages in either project using the configured NuGet source.
- Packed and installed `BrownPants.Cli` 0.4.0 from the local artifact using the repository's source mapping and NuGet trusted-signers policy. The installed tool passed a temporary TypeScript fixture: it reported two declarations, one syntax-resolved module relationship, and one call relationship.
- All eight packed-tool smoke scripts passed: model routing, GitHub PR, multi-provider PR, WinForms, native flow, native impact, managed interop, and impact graph coverage. Packed PNG and diagrams.net XML output passed signature, node/edge, and no-external-content checks.
- The packed JavaScript/TypeScript smoke script passed against the locally installed package, resolving a declared workspace import and generic call in JSON and reporting the evidence section in Markdown. CI now runs this same check for each OS.
- BrownPants-on-BrownPants inspection passed using the installed package in Markdown and JSON; both formats included the JS/TS evidence section and JSON retained `SchemaVersion: 1`.
- The packed diagram smoke exposed an output collision because PNG rendering already saves a `.drawio` sidecar. CI now passes `--overwrite` for the subsequent explicit XML render, and the corrected command passed locally.

## Release candidate checks pending

- Run the release commit through Windows, Ubuntu, and macOS CI and verify tag artifact validation before publishing the GitHub release.

The checks above establish local implementation and packaging behavior. They do not establish cross-platform release readiness until CI completes.
