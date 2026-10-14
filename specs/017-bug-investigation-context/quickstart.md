# Quickstart: Bug Investigation Context

Prerequisites: .NET 10 SDK. From the repository root:

1. Build and run all tests:
   ```powershell
   dotnet build BrownPants.sln --configuration Release
   dotnet test BrownPants.sln --configuration Release
   ```
2. Investigate explicit clues in a repository:
   ```powershell
   dotnet run --project src/BrownPants.Cli -- investigate . --clue "TimeoutException" --clue "ERR-42" --format markdown
   ```
3. Request machine-readable output with a smaller sample:
   ```powershell
   dotnet run --project src/BrownPants.Cli -- investigate . --clue "TimeoutException" --format json --limit 20
   ```

Expected behavior: results list matching paths and content line numbers, label likely test paths, show unmatched clues and omitted counts, and state that matches do not establish a bug's cause or test coverage. No source code, build, test, or network operation runs as part of `investigate` itself.

Clues are literal, case-insensitive strings; provide at most 20 clues of 1000 characters each. Binary files and files larger than 10 MiB are skipped for content matching and appear in read-issue diagnostics. Their paths can still match clues.

Temporary-repository tests should include path and content matches, a clue with no match, test-path classification, ignored directories, LF/CRLF line numbers, unreadable files where the host permits them, Markdown/JSON parity, invalid arguments, and stable output across repeated runs.

## Verification record

Verified on 2026-10-14:

- `dotnet build BrownPants.sln --configuration Release` — passed with 0 warnings and 0 errors.
- `dotnet test BrownPants.sln --configuration Release` — passed, 56 tests.
- `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1` — passed, including live Spec Kit extension checks.
- `pwsh -NoProfile -File tests/OpenSpecInstallerTests.ps1` — passed.
- Release package install smoke check — passed for `--help`, `inspect --format json`, and `investigate --format json`.
- `git diff --check` — passed.
