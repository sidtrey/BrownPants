# Verification: Local Text Flowcharts

## Acceptance criteria

- Acceptance criteria digest: `41a11f4aacc437c294b83ef22a2dd1f96c0e57c73ca65641bd69ccb10df113ab` (verified with `scripts/acceptance-criteria-hash.ps1`).
- Evidence ledger structure: passed for 7 tasks and 11 criteria using `scripts/check-acceptance-evidence.ps1`.

## Build and tests

- `dotnet restore BrownPants.sln --locked-mode`: passed on Windows and in the .NET 10 Linux SDK container.
- `dotnet build BrownPants.sln -c Release --no-restore`: passed on Windows and Linux with 0 warnings and 0 errors.
- Focused flowchart tests: 20 passed on Windows and Linux.
- Full suite: 161 passed on Windows and Linux.
- Spec Kit extension, OpenSpec installer, Copilot prompt, and agent skill integration scripts: passed.

The Linux image has an older .NET 10 SDK whose `dotnet test` reports no test projects for this Microsoft.Testing.Platform solution. The same built test assembly ran directly in the container and all tests passed; Windows also passed through `dotnet test`.

## Output and package checks

- Packed `BrownPants.Cli` installed and ran its help, PNG, and draw.io commands using a temporary source-mapped NuGet configuration with signature validation required and nuget.org trusted certificates pinned. CI now smoke-tests both package outputs and checks the PNG signature and expected XML graph cells.
- Generated PNG and `.drawio` files on Windows and Linux had identical SHA-256 hashes (`2C4C4EADA453D30F8F918BA78F633AEFC5EE804912A3E3579538CF47C0E99CC2` and `71562751C3D29DFC0531CB60E18E26F2E2C324780C4347EEAC1367659E193AF2`, respectively).
- The output `.drawio` XML passed validation against the diagrams.net `mxfile.xsd`; tests also verify well-formed XML, graph cells, escaping, and absence of scripts/external references.
- Manual PNG review confirmed readable node/edge labels and routed cycle edges. Renderer remains local, deterministic, and dependency-free.
