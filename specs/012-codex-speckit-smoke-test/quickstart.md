# Quickstart: Verify Spec Kit Setup with Codex

1. Install Spec Kit CLI 1.2.0 or another supported CLI version.
2. From the repository root, set `$env:BROWNPANTS_REQUIRE_SPECKIT='true'` in PowerShell.
3. Run `./tests/SpeckitExtensionTests.ps1`.
4. Expect the live integration check to initialize Codex skills, install BrownPants, and pass all ten hook assertions.
5. CI repeats this check on Windows, macOS, and Ubuntu.
