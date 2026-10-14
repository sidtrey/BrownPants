# Research: Live Spec Kit Integration Test

## Decision: Validate by installing the local extension with the real CLI

Spec Kit's development install accepts a local extension directory with `specify extension add <path> --dev`. It registers the extension and writes generated extension files plus `.specify/extensions.yml` entries.

**Rationale**: Static regex checks do not exercise Spec Kit's manifest validation, installation, and registration behavior.

**Alternatives considered**: A mock parser or hand-authored registration fixture would not test the actual installer.

Source: [Spec Kit extension reference](https://github.com/github/spec-kit/blob/main/extensions/EXTENSION-API-REFERENCE.md).

## Decision: Pin `specify-cli` 1.2.0 in CI

CI installs the exact CLI version currently supported and used for the local live installation proof.

**Rationale**: Pinning avoids unplanned integration changes while still testing the supported Spec Kit behavior on every hosted OS.

**Alternatives considered**: Installing latest would make CI behavior change without a repository update; skipping CLI installation in CI leaves live compatibility unverified.

Source: [Spec Kit installation guide](https://github.com/github/spec-kit/blob/main/docs/installation.md).

## Decision: Skip only when the CLI is absent in a local invocation

The adapter test script detects `specify` and reports an explicit skip locally when absent. CI installs it before running the same test script, so CI cannot silently skip the live test.

**Rationale**: Contributors can run static tests without installing Python tooling, while hosted CI still proves actual compatibility.
