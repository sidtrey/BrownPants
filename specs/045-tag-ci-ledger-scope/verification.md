# Verification Evidence: Release Tag Acceptance Ledger Scope

Date: 2026-10-15

- pwsh -NoProfile -File tests/AcceptanceCriteriaIntegrityWorkflowTests.ps1 — PASS. A temporary Git history verifies ordinary branch before/head diffs, new tag parent diffs, initial branch full-tree fallback, root-tag full-tree fallback, pull request base/head diffs, and empty manual-dispatch changes.
- dotnet build BrownPants.sln -c Release --no-restore — PASS; 0 warnings, 0 errors.
- dotnet test BrownPants.sln -c Release --no-restore — PASS; 235/235 tests.
- dotnet list BrownPants.sln package --vulnerable --include-transitive — PASS; no vulnerable packages reported.
- dotnet pack src/BrownPants.Cli -c Release --no-build -o ./artifacts — PASS; created BrownPants.Cli.0.2.1.nupkg, SHA-256 $packageHash.
- Local package install and packed CLI smoke scripts for provider routing, GitHub gating, model profiles, and WinForms — PASS.
- Docker --network none installed the local 0.2.1 package using only ./artifacts and cached NuGet packages; packed inspect --format json and PR help both ran successfully.
- High-confidence credential-pattern scan of recent source changes and the 0.2.1 package — 0 matches.
- Acceptance criteria SHA-256 — 9233465fd164d442aa4323e90a9755fd8ba34a8c78f67611d13ae7ecaedad9c9.

## Published release

- Branch CI for commit `18644aa4c82640974ab2d553838b9bef759c84fb` — PASS on Windows, Ubuntu, and macOS: https://github.com/sidtrey/BrownPants/actions/runs/38694358975
- Tag CI for `v0.2.1` — PASS on Windows, Ubuntu, and macOS, including acceptance ledger selection and packed-tool smoke: https://github.com/sidtrey/BrownPants/actions/runs/38694648982
- GitHub release: https://github.com/sidtrey/BrownPants/releases/tag/v0.2.1
- Uploaded `BrownPants.Cli.0.2.1.nupkg` digest matches the local package SHA-256 `1130da1fb2bf9287f650f814428f4c735c6b259c76fc6c2da4d9f0fa0793c9ab`.
