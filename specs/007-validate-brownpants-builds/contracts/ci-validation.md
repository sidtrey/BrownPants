# CI Validation Contract

The required CI check runs on Windows, Linux, and macOS. Each host independently performs:

1. .NET 10 restore and Release build of `BrownPants.sln`.
2. The full Release xUnit suite.
3. `tests/SpeckitExtensionTests.ps1` and `tests/OpenSpecInstallerTests.ps1` under PowerShell Core.
4. Release tool packaging and local tool installation.
5. Invocation of the installed platform-specific command with `inspect . --format json --limit 10`.

The smoke check succeeds only when the command exits successfully, JSON parses, `SchemaVersion` is integer `1`, and `FileCount` is greater than zero. Windows uses the `.exe` shim; Linux and macOS use the extensionless shim. Matrix failures remain independently visible.
