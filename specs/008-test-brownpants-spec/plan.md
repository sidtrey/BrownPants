# Implementation Plan: Test BrownPants Spec Kit Extension Installation

**Branch**: `008-test-brownpants-spec` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

## Summary

Extend the Spec Kit adapter test script with an isolated live installation check. When `specify` is available, install the local extension using `specify extension add <path> --dev`, then verify installed files and the generated project hook registrations. CI installs pinned `specify-cli` 1.2.0 before running the existing adapter check on all supported hosts.

## Technical Context

**Language/Version**: PowerShell Core; Python 3.12 for Spec Kit CLI installation
**Primary Dependencies**: Spec Kit CLI `specify-cli` 1.2.0, GitHub Actions setup-python, existing test scripts
**Storage**: Temporary filesystem fixture only
**Testing**: `tests/SpeckitExtensionTests.ps1` plus CI on Windows, Linux, and macOS
**Target Platform**: Windows, Linux, macOS
**Project Type**: CLI adapter validation and CI
**Performance Goals**: Integration check completes within ordinary CI job time
**Constraints**: Never modify repository `.specify` state; skip locally with a clear message if `specify` is unavailable; clean up only the generated temp fixture
**Scale/Scope**: One local extension install, ten lifecycle hooks, one command file

## Constitution Check

- **Evidence First**: Validate actual Spec Kit installation artifacts and registration, not presumed compatibility. PASS.
- **Broad, Engine-Neutral Core**: Scope remains in the Spec Kit adapter; core inventory behavior is untouched. PASS.
- **Deterministic and Reviewable Output**: Pin the CLI version in CI and assert exact hook counts/targets. PASS.
- **Safe Inspection by Default**: The installer copies declarative extension files into an isolated fixture; no repository project is evaluated. PASS.
- **Specified and Verified Changes**: The spec defines installation behavior and cross-platform evidence. PASS.

## Research Summary

Spec Kit documents PyPI installation of `specify-cli` and local development extension installation through `specify extension add <path> --dev`. Its installer creates project registration and extension files. A pinned CLI provides repeatable CI behavior. Sources: [Spec Kit installation guide](https://github.com/github/spec-kit/blob/main/docs/installation.md), [Spec Kit extension reference](https://github.com/github/spec-kit/blob/main/extensions/EXTENSION-API-REFERENCE.md).

## Design

The live check is optional for standalone local execution when `specify` is absent, but required in CI after installing the pinned CLI. The test creates a unique temporary `.specify` project, runs the real installer from that directory, then checks the generated `extensions.yml`, installed manifest, command file, enabled extension listing, and exact hook registrations. A `finally` block restores the working directory and removes only the unique fixture under the system temporary directory.

### Expected Repository Changes

```text
.github/workflows/ci.yml           # Install pinned Spec Kit CLI before adapter checks
tests/SpeckitExtensionTests.ps1     # Run optional live install verification
integrations/speckit/README.md      # Document live integration test behavior
```

## Constitution Check After Design

All principles pass. The check uses an isolated project and a pinned external CLI, preserves repository-local registration, and makes no changes to scanner semantics.
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
