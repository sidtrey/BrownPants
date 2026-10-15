# Verification: Azure DevOps Sprint and Delivery Traceability

Implementation has not started. Acceptance criteria are frozen in `acceptance-criteria.sha256`; task evidence is planned in `acceptance-evidence.json`.

## Planned verification

- Mocked HTTP contract tests will verify API versioning, allowlisted hosts, token redaction, read-only trace calls, pagination/error diagnostics, deterministic reports, and no network access without `--online`.
- PR submission tests will assert that only a confirmed draft-PR create request is sent and that `isDraft` is true with no reviewer or completion fields.
- Release build, full xUnit suite, integration test, package smoke, acceptance digest/evidence checks, and Windows/macOS/Linux hosted CI remain required before T017 can pass.

## External behavior reviewed

Microsoft Learn documents the versioned work-item, build, release, and Git pull-request REST endpoints referenced in `research.md`. Repository tests cannot validate a customer's Azure DevOps permissions, custom process, retention policies, or tenant authentication rules.
