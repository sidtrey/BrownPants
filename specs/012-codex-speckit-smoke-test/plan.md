# Implementation Plan: Verify Spec Kit Setup with Codex

**Branch**: `012-codex-speckit-smoke-test` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/012-codex-speckit-smoke-test/spec.md`

## Summary

Extend the live Spec Kit extension installer test to initialize a temporary Codex project before installing BrownPants. Assert generated Spec Kit skills and the existing enabled ten-hook extension registration.

## Technical Context

**Language/Version**: PowerShell 7+ and Spec Kit CLI 1.2.0 in CI
**Primary Dependencies**: Spec Kit CLI; no Codex executable needed
**Storage**: Temporary filesystem fixture, removed after test
**Testing**: `tests/SpeckitExtensionTests.ps1`
**Target Platform**: Windows, Linux, macOS
**Project Type**: Cross-platform .NET CLI with PowerShell integration checks
**Performance Goals**: Reuse the existing live installer job; no separate setup process
**Constraints**: Non-interactive CLI; safe temporary path validation; no repository writes
**Scale/Scope**: Verify representative Codex skills plus all ten BrownPants hooks

## Constitution Check

- Evidence First: assert generated skill paths and installed hook registration. PASS.
- Broad, Engine-Neutral Core: change only validates the Codex adapter. PASS.
- Deterministic and Reviewable Output: use fixed expected skill names and hook count. PASS.
- Safe Inspection by Default: fixture is temporary, bounded, and cleaned up. PASS.
- Specified and Verified Changes: this feature has an independently runnable live test. PASS.

## Design

The test creates a unique directory under the system temp root, runs Spec Kit initialization with `--integration codex`, `--integration-options=--skills`, `--non-interactive`, and `--ignore-agent-tools`, and then installs the local extension. It asserts key skill directories and the existing manifest, command, and hook registration checks. Required CI continues to enforce Spec Kit CLI presence; local runs can retain the existing optional skip behavior.

## Validation

Run `tests/SpeckitExtensionTests.ps1` with `BROWNPANTS_REQUIRE_SPECKIT=true`, the full .NET build/test suite, OpenSpec adapter checks, and the Windows/macOS/Ubuntu CI matrix.
