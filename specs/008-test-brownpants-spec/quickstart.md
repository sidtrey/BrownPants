# Quickstart: Test the Spec Kit Extension Installer

## Prerequisites

- PowerShell Core
- Spec Kit CLI 1.2.0 (`specify`)

## Validate

```powershell
./tests/SpeckitExtensionTests.ps1
```

The script runs static manifest synchronization checks and installs the extension into an isolated temporary project through the actual Spec Kit CLI. CI provisions the pinned CLI and runs the check on Windows, Linux, and macOS. See [the integration test contract](contracts/integration-test.md).
