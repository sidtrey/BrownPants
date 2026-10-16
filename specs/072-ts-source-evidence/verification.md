# Verification: JavaScript and TypeScript Source Evidence

## Implementation checks

- `dotnet build BrownPants.sln -c Release --no-restore` passed with zero warnings and errors; `scripts/test.ps1 -Configuration Release` passed all 386 tests.
- QA regressions verify same-class-only `this.method()` resolution (including duplicate class names), imported parameter, local-variable, destructured-binding, catch-parameter, and function-scoped `var` shadowing, generic parameters with return annotations, control-flow blocks that must not look like callable scopes, indexed imported-call resolution, exact `*` and recursive `**` workspace glob depth, each of the eight advertised extensions, and visible aggregate source, token, record, metadata-byte, diagnostic, and early-stop file-count limits.
- A QA-role review of the final analyzer diff found no release-blocking issue. It noted the remaining general limitation that token-based scope handling is an approximation for less common JS/TS syntax.
- `tests/TestRunArtifactsTests.ps1` passed; the wrapper also passed and cleaned its managed test run while preserving all 524 legacy unmanaged `TestResults` entries.
- Cyclic barrel export coverage verifies two resolvable module edges, a deterministic report, and an unresolved imported call with no fabricated target.
- Acceptance hash verified: `1fb91210eebfc19a53e4883965a3585abffb4a4023c15339b04bd454d98cab52`.
- Spec Kit extension, OpenSpec installer, Copilot prompt, Cursor command, Codex usage parser, agent skill, acceptance integrity, acceptance hash, and test artifact cleanup scripts passed.
- `dotnet restore BrownPants.sln --locked-mode` passed. `dotnet list BrownPants.sln package --vulnerable --include-transitive` reported no vulnerable packages in either project using the configured NuGet source.
- Packed and installed `BrownPants.Cli` 0.4.0 from the current Release build using repository source mapping and NuGet trusted-signers policy. The installed tool passed a temporary TypeScript fixture: it reported two declarations, one syntax-resolved module relationship, and one call relationship.
- All nine packed-tool smoke scripts passed against the package rebuilt from the current Release output: model routing, GitHub PR, multi-provider PR, WinForms, native flow, native impact, managed interop, impact graph coverage, and JavaScript/TypeScript workspace/call resolution. The package installed under repository NuGet source mapping and trusted-signer policy. Packed PNG and diagrams.net XML output passed signature, node/edge, and no-external-content checks in the CI workflow and previous local smoke run.
- The packed JavaScript/TypeScript smoke script passed against the locally installed package, resolving a declared workspace import and generic call in JSON and reporting the evidence section in Markdown. CI now runs this same check for each OS.
- BrownPants-on-BrownPants inspection passed using the installed package in Markdown and JSON; both formats included the JS/TS evidence section and JSON retained `SchemaVersion: 1`. The packed JSON inspection reported 1,083 files and zero JS/TS source files in this C# repository, as expected.
- The packed diagram smoke exposed an output collision because PNG rendering already saves a `.drawio` sidecar. CI now passes `--overwrite` for the subsequent explicit XML render, and the corrected command passed locally.

## Release candidate checks pending

- Run the updated release commit through Windows, Ubuntu, and macOS CI and verify tag artifact validation before publishing the GitHub release. The previous 809b9d run exposed the cleanup-script exit-code bug fixed above; one Ubuntu semantic test also failed once and passed on the parallel rerun, so the updated CI must confirm cross-platform stability.

The acceptance hash remains `1fb91210eebfc19a53e4883965a3585abffb4a4023c15339b04bd454d98cab52`; the evidence validator passed for 7 tasks and 10 criteria. These checks establish local implementation and packaging behavior. They do not establish cross-platform release readiness until updated CI completes and the release tag artifact is validated.
