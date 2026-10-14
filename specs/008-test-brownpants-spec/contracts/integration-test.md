# Spec Kit Integration Test Contract

When `specify` is available, `tests/SpeckitExtensionTests.ps1` MUST:

1. Create a clean project under the system temporary directory.
2. Run `specify extension add <repo>/integrations/speckit --dev` in that project.
3. Require a successful process exit, an enabled BrownPants extension listing, an installed manifest, and the command file declared by that manifest.
4. Verify one enabled mandatory `speckit.brownpants.context` registration for each of the ten lifecycle events declared by the extension.
5. Restore the original working directory and remove only its generated project root, including on failure.

If `specify` is absent during local use, report the live check as skipped. CI installs `specify-cli` 1.2.0 and sets `BROWNPANTS_REQUIRE_SPECKIT=true` before running the test; absence MUST fail the CI check rather than skip.
