# BrownPants Spec Kit extension

This extension adds `speckit.brownpants.context` and runs it automatically before Spec Kit's constitution, specify, clarify, plan, checklist, tasks, analyze, implement, and task-to-issues commands, and after implementation. It also provides the optional `speckit.brownpants.bug-evidence` hook for Spec Kit's bundled `bug` extension. When accepted, that hook searches safe clues already pasted into the report and saves bounded navigation leads beside the assessment. It never fetches URLs, includes source snippets, or asserts root cause; the official assess/fix/test commands remain unchanged. The post-hook refresh supports completion review against the active task list and current repository; it does not replace build or test validation. The context uses BrownPants' deterministic, cross-ecosystem inventory as a starting map, distinguishes inventory-only languages from Roslyn and lexical analysis, then instructs the agent to verify claims against source files.

When requested, the context command can map instruction files with `brownpants instructions` and guide creation of a cited response frame with `brownpants frame create`; `brownpants frame validate` checks source freshness. The frame preserves links to canonical details and carries host-loading caveats. It does not replace Spec Kit's constitution/spec workflow or automatically change agent host configuration.

## Build the manifest

Spec Kit requires the extension repository URL and license identifier in `extension.yml`. Generate that file from the checked-in template:

```powershell
.\integrations\speckit\BuildExtension.ps1 -RepositoryUrl <https-url> -License <identifier>
```

Pack and install BrownPants in the target repository. A local tool path works with the context command’s `.tools` fallback:

```powershell
dotnet pack src/BrownPants.Cli -c Release -o ./artifacts
dotnet tool install --tool-path ./.tools --add-source ./artifacts BrownPants.Cli --version 0.3.0
```

Initialize the project with Spec Kit, then install this local extension:

```powershell
specify init --here --integration <agent>
specify extension add .\integrations\speckit --dev
specify extension list
```

For Codex CLI, use `specify init --here --integration codex --integration-options="--skills"`. Spec Kit 1.0+ installs command skills in `.agents/skills`; the BrownPants extension refreshes context before nine core lifecycle commands and after implementation. The optional bug-assessment hook requires the bundled `bug` extension's `before_bug_assess` dispatch; the live integration test verifies it with Spec Kit CLI 1.2.0. It uses the global `brownpants` command or repo-local `.tools` shim documented in each command.

The active agent must be restarted or refreshed if its command list does not update. The `--semantic` scan is not automatic because MSBuild project evaluation may execute imported targets.

Run the hook and guidance checks from the BrownPants repository with `pwsh -NoProfile -File tests/SpeckitExtensionTests.ps1`. When the Spec Kit `specify` CLI is installed, this initializes Codex skills in a unique temporary project, installs the bundled bug extension and BrownPants, and checks the generated skills, extension files, and all hook registrations. The live check does not require Codex itself. Without `specify`, only the live check is skipped. CI installs Spec Kit CLI 1.2.0 and runs the live check on Windows, Linux, and macOS.
