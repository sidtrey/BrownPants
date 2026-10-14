# Quickstart: Cross-Platform Validation

## Prerequisites

- .NET 10 SDK
- PowerShell Core
- Windows, Linux, or macOS

## Local checks

```powershell
dotnet restore BrownPants.sln
dotnet build BrownPants.sln --configuration Release --no-restore
dotnet test BrownPants.sln --configuration Release --no-build
./tests/SpeckitExtensionTests.ps1
./tests/OpenSpecInstallerTests.ps1
dotnet pack src/BrownPants.Cli --configuration Release --no-build --output ./artifacts
```

Install the package into a temporary tool path, invoke `brownpants.exe` on Windows or `brownpants` on Linux/macOS, and verify JSON schema version `1` and a nonzero file count. The workflow runs this smoke check independently on Windows, Linux, and macOS. The CI contract is in [ci-validation.md](contracts/ci-validation.md).
