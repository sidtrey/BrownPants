# Verification: Filter Test Methods from Factory Signals

**Acceptance criteria digest**: `3a43a38561140ddef870e2e2dcc3c05a9e31caa03d8225503735ee428913c87a` (`spec.md`; verified before and after implementation)

## Automated checks

- `dotnet test BrownPants.sln -c Release --filter FullyQualifiedName~RepositoryScannerTests` — passed, 69 tests.
- `dotnet build BrownPants.sln -c Release` — passed, 0 warnings and 0 errors.
- `dotnet test BrownPants.sln -c Release` — passed, 141 tests.
- `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/031-filter-test-factory-signals/spec.md` — passed; digest unchanged.
- `pwsh -NoProfile -File scripts/check-acceptance-evidence.ps1 -RepositoryRoot . -SpecPath specs/031-filter-test-factory-signals/spec.md -TasksPath specs/031-filter-test-factory-signals/tasks.md -EvidencePath specs/031-filter-test-factory-signals/acceptance-evidence.json` — passed; all 4 tasks map to all 6 acceptance IDs.

## Acceptance evidence

- **AC-001 / SC-001**: Temporary C# and VB fixtures cover xUnit `Fact`/`Theory`, NUnit `Test`/`TestCase`/`TestCaseSource`, and MSTest `TestMethod`/`DataTestMethod`, including qualified names, `Attribute` suffixes, arguments, and multiple attributes. The fixture report contains no factory-method findings for those seven test methods in either language.
- **AC-002 / SC-002**: C# and VB production factories with XML comments and `Obsolete` attributes remain candidates. JSON identifies their declaration lines (22 and 35 in the fixture) and signature evidence, without attribute text. Markdown reports the same paths, lines, and signatures.
- **AC-003**: Existing `Scan_ExcludesStringHelpersAndKeepsObjectFactoryMethodsInCSharpAndVisualBasic` passes within the full scanner suite, preserving string/void exclusions and object factory findings.
- **SC-003**: Semantic self-inspection via `dotnet run --project src/BrownPants.Cli -- inspect . --format json --semantic --limit 100` and the corresponding Markdown command reports three factory candidates. Neither `CreateDraftAsync_SendsOneDraftCreateRequestWithExplicitWorkItems` nor `CreateDraftAsync_RejectsEditedPreviewWithoutNetworkRequest` appears as a factory finding. The reports retain legitimate implementation and helper candidates.

The self-inspection uses semantic mode to reproduce the earlier baseline; the feature itself requires only syntax parsing. All pattern findings remain candidates, not resolved semantic relationships.
