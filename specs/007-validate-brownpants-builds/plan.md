# Implementation Plan: Validate BrownPants on Windows, Linux, and macOS

**Branch**: `007-validate-brownpants-builds` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Run the existing Release build, xUnit suite, adapter scripts, package creation, and installed CLI smoke check independently on Windows, Linux, and macOS. Adapt only the installed executable name to the host; keep validation steps and report assertions equivalent. GitHub Actions officially supports OS matrix jobs and independent failure behavior through `fail-fast: false`.

## Technical Context

**Language/Version**: C# and PowerShell Core; .NET 10
**Primary Dependencies**: Existing GitHub Actions checkout/setup-dotnet actions and repository projects
**Storage**: N/A
**Testing**: xUnit, `tests/SpeckitExtensionTests.ps1`, `tests/OpenSpecInstallerTests.ps1`, and packed CLI smoke
**Target Platform**: Windows, Linux, macOS hosted runners
**Project Type**: .NET global tool and repository CI
**Performance Goals**: Complete the same validation stages on each host; no timing target
**Constraints**: Keep Windows coverage; avoid building Windows-only MFC fixtures; do not add runtime dependencies
**Scale/Scope**: One validation workflow, three operating systems, existing test and adapter suites

## Constitution Check

- **Evidence First**: CI validates observable CLI output and does not infer MFC buildability on non-Windows systems. PASS.
- **Broad, Engine-Neutral Core**: Platform coverage is independent of SDD engine and repository ecosystem. PASS.
- **Deterministic and Reviewable Output**: All hosts assert the same report schema and non-empty inventory. PASS.
- **Safe Inspection by Default**: Smoke checks use default static inspection; no project evaluation or network access from the inspected repository. PASS.
- **Specified and Verified Changes**: This plan follows the feature spec and will add workflow validation where needed. PASS.

## Research Summary

GitHub Actions matrix jobs run a job once per configured OS; disabling fail-fast preserves results when one host fails. Hosted runner labels exist for Windows, Ubuntu, and macOS. Existing PowerShell scripts use cross-platform PowerShell Core. Tool installation creates a platform-specific shim, so the smoke script must select `.exe` only on Windows. Sources: [GitHub matrix jobs](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/run-job-variations), [workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax), and [runner images](https://github.com/actions/runner-images).

## Design

No application data model or public API changes are required. The validation contract defines the same commands and success evidence for each host. Only the path to the installed tool command differs by operating system. The existing xUnit fixture for Windows-specific project metadata verifies those files remain inspectable from every host.

### Expected Repository Changes

```text
.github/workflows/ci.yml                 # Add independent OS matrix and portable smoke assertions
tests/SpeckitExtensionTests.ps1           # Adjust only if cross-host assumptions are found
tests/OpenSpecInstallerTests.ps1          # Adjust only if cross-host assumptions are found
specs/007-validate-brownpants-builds/     # Feature evidence and validation guide
```

## Constitution Check After Design

All five principles remain satisfied. The matrix adds validation only; it does not alter scanner behavior or make host OS a condition for inspecting repository artifacts.
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
