# Feature 053 Verification: Scope-Aware WinForms Control Evidence

## Acceptance evidence

- **AC-001 / SC-001**: C# partial `BaseScreen` declarations split across files resolve inherited `Screen : BaseScreen` writes (`this.Text`, `_caption.Text`) and a typed chained member (`_holder.Button.Text`). Visual Basic partial `Screen` declarations split across files resolve inherited `Me.Text` and a field declared in another file.
- **AC-002 / SC-001 / SC-002**: C# and VB nested locals named `shadowed` resolve independently; the inner `Model.Text` write is excluded while the Control write is retained.
- **AC-003 / SC-001**: C# fixtures cover a `using` alias, a fully qualified WinForms type, typed fields/properties across files, a chained member path, and `new()` inferred from the declared variable type.
- **AC-004 / SC-002**: VB fixture covers cross-file type/member indexing, chained member resolution, nested local scope, and ordinary-object exclusion.
- **AC-005**: Existing malformed-source test retains parse diagnostics. Dynamic receiver fixture reports one `BPWF100` coverage issue and does not promote it to a Control candidate. Namespace-scoped import test proves Windows Forms imports do not leak into an unrelated namespace.
- **AC-006 / SC-003**: Repeated analyzer JSON is byte-stable for the same input; limit tests reconcile candidate/omitted counts. Existing source-only fixture checks no report excerpts and no repository writes.
- **SC-004**: Packed CLI smoke includes `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>`, analyzes two split C# files, finds both expected writes, indexes one partial type, and leaves the fixture with exactly its three authored files.
- **SC-005**: BrownPants self-analysis parsed 78 C#/VB files, indexed 93 source types, and reported 0 candidates, 0 parse errors, and 0 unresolved receivers. The legacy Framework fixture parsed its one C# file and indexed two types with 0 parse errors or findings. These repositories contain no WinForms controls, so positive Control detection is established by the explicit C#/VB fixtures and packed smoke.

## Checks run

- TDD baseline: before implementation, the new C# fixture found 1 of 4 expected writes and the VB fixture found 0 of 2.
- `dotnet test tests/BrownPants.Tests/BrownPants.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~WinFormsUiThreadingTests` — passed, 9 tests, including derived control inheritance across partial files and same-named scope decoys.
- `dotnet build BrownPants.sln -c Release --no-restore` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release --no-restore` — passed, 262 tests, 0 failed, 0 skipped.
- `dotnet pack src/BrownPants.Cli/BrownPants.Cli.csproj -c Release --no-restore -o <temporary-directory>` — passed; extracted packed CLI ran `tests/PackedWinFormsSmoke.ps1` successfully, including the legacy .NET Framework project metadata case.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/053-scope-aware-winforms-evidence/spec.md` — verified `005aef11ae7f09e05ac48f1e4131417945fb39a5b4cf48cfdb5dab9721d54d13`.
- Acceptance evidence ledger validator — passed for 9 tasks and 11 criteria.
- `git diff --check` — passed.

## Limits

This remains source-only syntax indexing, not compiler binding. It does not evaluate MSBuild conditions, global imports from other files, reflection/dynamic control creation, runtime caller threads, or marshaling success. Unresolved counts measure assignment receiver type evidence only within likely WinForms source contexts; they are not defect counts.
