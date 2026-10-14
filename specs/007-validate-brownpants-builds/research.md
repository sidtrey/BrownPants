# Research: Cross-Platform Validation

## Decision: Use an independent three-OS workflow matrix

GitHub Actions matrix jobs allow one validation definition to run against Windows, Ubuntu, and macOS. Set `fail-fast: false` so one host failure does not cancel other host jobs.

**Rationale**: The checks must remain equivalent while exposing each host's result independently.

**Alternatives considered**: Separate duplicated jobs would diverge over time; keeping Windows-only CI leaves portability unverified.

Source: [GitHub matrix jobs](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/run-job-variations), [workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax).

## Decision: Keep the packed tool smoke check host-aware

The tool installation directory contains `brownpants.exe` on Windows and `brownpants` on Unix-like hosts. Invoke that path directly and validate JSON `SchemaVersion` 1 as well as a non-empty file count.

**Rationale**: This proves the packaged artifact, command shim, serializer, and scanner work together on each OS.

**Alternatives considered**: Running `dotnet run` would bypass the packaged command shim; checking only `--help` would not verify inventory output.

Source: [dotnet tool install](https://learn.microsoft.com/dotnet/core/tools/dotnet-tool-install).

## Decision: Run the existing PowerShell adapter checks on all hosts

Use PowerShell Core already available in the selected GitHub-hosted images; keep both adapter test scripts in each matrix job.

**Rationale**: These scripts validate static templates and installation behavior, so they are part of the supported contributor workflow across platforms.

**Alternatives considered**: Restricting them to Windows would leave cross-host paths and filesystem behavior unverified.
