# Implementation Plan: Task-Based Model Guidance

**Branch**: `038-model-routing-policy` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

## Summary

Add an optional JSON model-routing policy and a local `brownpants models` command to validate, report, and explicitly export task profiles as native VS Code/Copilot, Visual Studio 18.4+, Claude Code, or Codex project agents. Team-selected model identifiers remain intact. Exported files are suggestions the host can invoke; BrownPants does not schedule work or guarantee availability.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: Existing BCL (`System.Text.Json`); no new NuGet packages
**Storage**: `.brownpants/models.json` plus explicitly requested host-local agent profiles
**Testing**: xUnit temporary repository tests, command-level output tests, full solution checks
**Target Platform**: Windows, Linux, macOS through .NET 10
**Project Type**: Local CLI
**Constraints**: No model/API/network calls, no account checks, no MSBuild evaluation, deterministic reports, root-contained reads and writes, conflict-preserving exports
**Scale/Scope**: Up to 64 profiles, 32 categories, 128 KiB policy file; export hosts are VS Code/Copilot, Visual Studio 18.4+, Claude Code, and Codex

## Constitution Check

- Evidence first: pass; report model configuration and distinguish it from host invocation/availability.
- Broad, engine-neutral core: pass; policy categories are SDD concepts and profile exports are optional host adapters.
- Deterministic and reviewable: pass; canonical profile/category ordering, text output, and exact model strings.
- Safe inspection by default: pass; validation/reporting are read-only; export is explicit, project-local, preflighted, and conflict-safe.
- Specified and verified changes: pass; AC/SC baseline, mapped tasks, temporary-repository tests, build and full tests are required.

## Design

Read a versioned policy at `.brownpants/models.json` using strict, bounded JSON parsing. A profile has a safe kebab-case ID, one or more supported SDD categories, one of the `economy`/`balanced`/`high` tiers, rationale, and per-host exact model names. `models validate` checks the policy; `models report` emits equivalent Markdown/JSON with host capability notes; `models export --host vscode|visual-studio|claude|codex` writes one native profile per policy profile beneath that host's project directory. Missing host model mappings, unknown hosts, and invalid policies fail before writes. Exports use deterministic encoders and perform a full preflight: identical output is a no-op, conflicts or path links are refused, and partial output is avoided.

Host destinations:

- VS Code/Copilot: `.github/agents/brownpants-<id>.agent.md`
- Visual Studio 18.4+: `.github/agents/brownpants-visual-studio-<id>.agent.md` (distinct from VS Code names because the hosts share the directory)
- Claude Code: `.claude/agents/brownpants-<id>.md`
- Codex: `.codex/agents/brownpants-<id>.toml`

Visual Studio and VS Code use the same project-local agent directory and `.agent.md` format. Their distinct generated filenames and display names keep exports from overwriting each other and make the intended host clear; Visual Studio model selection is available only when the selected model exists in that host version/account. Visual Studio versions before 18.4 remain manual/unverified.

## Project Structure

```text
src/BrownPants.Cli/Analysis/ModelRoutingPolicy.cs
src/BrownPants.Cli/Analysis/ModelRoutingReportRenderer.cs
src/BrownPants.Cli/Analysis/ModelRoutingProfileExporter.cs
src/BrownPants.Cli/Program.cs
tests/BrownPants.Tests/ModelRoutingPolicyTests.cs
tests/BrownPants.Tests/ModelRoutingCommandTests.cs
tests/BrownPants.Tests/ModelRoutingProfileExporterTests.cs
docs/model-routing.md
examples/model-routing-policy.json
```

**Structure Decision**: Keep policy parsing, report rendering, and agent serialization in separate analysis services under the existing CLI project. Use isolated temporary repositories for all file-safety and command tests.

## Risks

- Host file schemas and support can change; cite the official documentation and keep the support statement dated.
- Shell/Markdown/YAML/TOML values need context-specific escaping; test adversarial names and multiline strings.
- A correctly generated profile can still be ignored or unavailable in the host; report this as a host decision, never as successful runtime enforcement.
