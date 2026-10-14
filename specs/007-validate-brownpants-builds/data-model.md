# Data Model: Cross-Platform Validation

## Host Validation Result

Represents one independent CI execution on a supported host.

- **OperatingSystem**: Windows, Linux, or macOS runner identity.
- **RestoreBuildResult**: restore and Release build outcome.
- **TestResult**: full xUnit suite outcome.
- **AdapterResults**: Spec Kit and OpenSpec adapter script outcomes.
- **PackageSmokeResult**: pack, install, CLI invocation, and JSON assertion outcome.
- **OverallResult**: success only when all required stages pass for that host.

The workflow retains a separate result for each host, including when another host fails.
