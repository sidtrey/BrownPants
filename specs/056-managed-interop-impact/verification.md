# Managed Interop Impact Verification

## Acceptance baseline

- Acceptance criteria digest: `e41ea376aea098eb719f1a4d14c293a12769e8670cdafefd79a4049abb582c3c` (`spec.md`). The baseline check passed before and after implementation; acceptance text was not changed.
- The acceptance evidence validator passed for 8 tasks and 11 criteria. It validates ledger structure and paths; behavior evidence is listed below.

## Build and fixture evidence

- `dotnet build BrownPants.sln -c Release` passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-build` passed: 272 tests, 0 failures, 0 skipped.
- Mixed-language scanner coverage verifies C# and VB target locations, generic typed handles, overloaded ambiguity with both locations, a known type with an unresolved method, and comment/string/wrong-type decoys. Repeated JSON and Markdown output matched; fixture file contents and file set remained unchanged.
- Managed impact coverage verifies the chain from a changed managed target through the C++/CLI bridge, qualified native caller, and MFC dispatch. Ambiguous target locations remain visible. Focused evidence selects these links separately from Roslyn-managed callers.

## Packaged CLI

- Packed `BrownPants.Cli` 0.2.1 after the final generic-identity change, installed it using the repository CI NuGet signature-validation and trusted-signer configuration, then ran `tests/PackedManagedInteropSmoke.ps1` against the installed executable. Installation and smoke passed.
- The smoke validates C# and VB target locations, overload candidates, decoy exclusion, managed-to-native/MFC impact, focused evidence selection, and unchanged fixture hashes.
- `tests/SpeckitExtensionTests.ps1` passed, including the live Spec Kit CLI integration check. `tests/OpenSpecInstallerTests.ps1` passed.

## BrownPants-on-BrownPants observations

- The current Release CLI inventory found 741 files, 3 native-flow relations, and 1 `cppcli-managed-call` relation targeting `LegacyOrderDesk.Workflows.OrderImportWorkflow<T>.ImportBatch`.
- Focused evidence for `examples/brownfield-bug-lab/managed/Workflows/OrderImportWorkflow.cs` selected 3 native candidates: the C++/CLI managed call, qualified native call, and MFC dispatch. It reported 2 completeness issues.
- `impact` for that same changed path returned the same 3 native relations, but 0 managed caller findings and 5 issues; the self-repository semantic graph was incomplete. This run verifies the cross-boundary candidates are surfaced, not a complete managed graph.
- A direct run against the Bug Lab returned the same source-located three-link chain and the expected managed semantic caller evidence. Its overall report remained incomplete because of 4 unresolved invocations and 1 workspace issue; those limitations were retained in the report.

## Limits

All C++/CLI-to-managed and native/MFC relationships remain lexical candidates. Matching source declarations does not prove project references, compiler binding, overload selection, runtime activation, or execution. The packed smoke fixture is an analysis fixture and does not compile MFC/C++/CLI code.
